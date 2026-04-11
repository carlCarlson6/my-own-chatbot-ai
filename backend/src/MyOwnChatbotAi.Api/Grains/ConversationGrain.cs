using Microsoft.Extensions.Options;
using MyOwnChatbotAi.Api.Contracts;
using MyOwnChatbotAi.Api.Features.Conversations.Persistence;
using MyOwnChatbotAi.Api.Services.Ollama;

namespace MyOwnChatbotAi.Api.Grains;

[GenerateSerializer]
public sealed class ConversationState
{
    [Id(0)] public string Title { get; set; } = string.Empty;
    [Id(1)] public string Model { get; set; } = string.Empty;
    [Id(2)] public List<ChatMessage> Messages { get; set; } = [];
    [Id(3)] public DateTime CreatedAtUtc { get; set; }
    [Id(4)] public bool IsInitialized { get; set; }
    [Id(5)] public string? OwnerUserId { get; set; }
    [Id(6)] public bool IsManagedConversation { get; set; }
    [Id(7)] public bool HasManualTitle { get; set; }
    [Id(8)] public DateTime UpdatedAtUtc { get; set; }
}

public sealed class ConversationGrain : Grain, IConversationGrain
{
    private sealed record PendingStreamMessage(string? OwnerUserId, Guid AssistantMessageId);

    private readonly IPersistentState<ConversationState> _state;
    private readonly IUserOwnedConversationStore _conversationStore;
    private readonly IOllamaClient _ollamaClient;
    private readonly OllamaOptions _options;
    private readonly ILogger<ConversationGrain> _logger;
    private PendingStreamMessage? _pendingStreamMessage;

    public ConversationGrain(
        [PersistentState("state", "conversations")] IPersistentState<ConversationState> state,
        IUserOwnedConversationStore conversationStore,
        IOllamaClient ollamaClient,
        IOptions<OllamaOptions> options,
        ILogger<ConversationGrain> logger)
    {
        _state = state;
        _conversationStore = conversationStore;
        _ollamaClient = ollamaClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CreateConversationResponse> InitializeAsync(string? ownerUserId, string title)
    {
        var conversationId = this.GetPrimaryKey();
        var isManagedConversation = !string.IsNullOrWhiteSpace(ownerUserId);

        if (_state.State.IsInitialized && HasAccess(ownerUserId))
        {
            _logger.LogDebug("Conversation {ConversationId} already initialized, returning existing state", conversationId);
            return CreateConversationResponse(conversationId);
        }

        if (isManagedConversation)
        {
            var persistedConversation = await _conversationStore.GetHistoryAsync(conversationId, ownerUserId!);
            if (persistedConversation is not null)
            {
                HydrateFromPersistedConversation(persistedConversation);
                await _state.WriteStateAsync();
                return CreateConversationResponse(conversationId);
            }
        }

        ResetState();

        var (normalizedTitle, hasManualTitle) = ConversationTitleGenerator.CreateInitialTitle(title);
        var now = DateTime.UtcNow;

        _state.State.Title = normalizedTitle;
        _state.State.Model = _options.DefaultModel;
        _state.State.CreatedAtUtc = now;
        _state.State.UpdatedAtUtc = now;
        _state.State.IsInitialized = true;
        _state.State.OwnerUserId = ownerUserId;
        _state.State.IsManagedConversation = isManagedConversation;
        _state.State.HasManualTitle = hasManualTitle;

        if (isManagedConversation)
        {
            await _conversationStore.CreateConversationAsync(
                new UserOwnedConversationSummary(
                    conversationId,
                    ownerUserId!,
                    _state.State.Title,
                    _state.State.HasManualTitle,
                    _state.State.Model,
                    _state.State.CreatedAtUtc,
                    _state.State.UpdatedAtUtc,
                    "active"));
        }

        await _state.WriteStateAsync();

        _logger.LogInformation(
            "Conversation {ConversationId} initialized with model '{Model}' and title '{Title}'",
            conversationId, _state.State.Model, normalizedTitle);

        return CreateConversationResponse(conversationId);
    }

    public async Task<SendMessageResponse?> SendMessageAsync(string? ownerUserId, ChatMessageInput message, bool createIfMissing)
    {
        EnsureNoPendingStream();

        var conversationId = this.GetPrimaryKey();

        if (!await EnsureConversationLoadedAsync(ownerUserId))
        {
            if (!createIfMissing)
            {
                _logger.LogDebug(
                    "SendMessage rejected for unavailable conversation {ConversationId} and user '{OwnerUserId}'",
                    conversationId,
                    ownerUserId ?? "anonymous");
                return null;
            }

            _logger.LogDebug(
                "Conversation {ConversationId} not yet initialized, auto-initializing before send",
                conversationId);
            await InitializeAsync(ownerUserId, string.Empty);
        }

        var previousTitle = _state.State.Title;
        var previousUpdatedAtUtc = _state.State.UpdatedAtUtc;
        var hadMessagesBeforeSend = _state.State.Messages.Count > 0;
        var userMessage = new ChatMessage(Guid.NewGuid(), "user", message.Content.Trim(), DateTime.UtcNow);
        _state.State.Messages.Add(userMessage);
        ApplyDefaultTitleFromFirstUserMessage(userMessage);

        var ollamaMessages = _state.State.Messages
            .Select(m => new OllamaMessage(m.Role, m.Content))
            .ToList();

        _logger.LogDebug(
            "Sending message to Ollama for conversation {ConversationId} using model '{Model}' ({MessageCount} messages in history)",
            conversationId, _state.State.Model, ollamaMessages.Count);

        var startedAt = DateTime.UtcNow;
        var model = _state.State.Model;
        string assistantContent;
        try
        {
            assistantContent = await _ollamaClient.ChatAsync(model, ollamaMessages);
        }
        catch (OllamaException ex)
        {
            _state.State.Messages.Remove(userMessage);
            _state.State.Title = previousTitle;
            _state.State.UpdatedAtUtc = previousUpdatedAtUtc;

            if (!hadMessagesBeforeSend)
            {
                await RollbackFailedEmptyManagedConversationAsync(conversationId);
            }

            _logger.LogError(ex,
                "Ollama failed to generate a reply for conversation {ConversationId} with model '{Model}'",
                conversationId, model);
            throw new InvalidOperationException(
                $"Ollama failed to generate a reply for model '{model}': {ex.Message}", ex);
        }

        var latencyMs = (int)(DateTime.UtcNow - startedAt).TotalMilliseconds;
        _logger.LogInformation(
            "Ollama responded for conversation {ConversationId} with model '{Model}' in {LatencyMs}ms",
            conversationId, model, latencyMs);

        var assistantMessage = new ChatMessage(Guid.NewGuid(), "assistant", assistantContent, DateTime.UtcNow);
        _state.State.Messages.Add(assistantMessage);
        _state.State.UpdatedAtUtc = assistantMessage.CreatedAtUtc;

        if (_state.State.IsManagedConversation && !string.IsNullOrWhiteSpace(_state.State.OwnerUserId))
        {
            await _conversationStore.AppendMessagesAsync(
                conversationId,
                _state.State.OwnerUserId!,
                _state.State.Title,
                _state.State.HasManualTitle,
                _state.State.UpdatedAtUtc,
                [
                    ToStoredMessage(userMessage),
                    ToStoredMessage(assistantMessage)
                ]);
        }

        await _state.WriteStateAsync();

        return new SendMessageResponse(
            conversationId,
            userMessage,
            assistantMessage,
            CreateConversationSummary(conversationId),
            model,
            "completed",
            latencyMs);
    }

    public async Task<StreamMessageStartResponse?> BeginStreamMessageAsync(
        string? ownerUserId,
        ChatMessageInput message,
        bool createIfMissing)
    {
        EnsureNoPendingStream();

        var conversationId = this.GetPrimaryKey();

        if (!await EnsureConversationLoadedAsync(ownerUserId))
        {
            if (!createIfMissing)
            {
                _logger.LogDebug(
                    "BeginStreamMessage rejected for unavailable conversation {ConversationId} and user '{OwnerUserId}'",
                    conversationId,
                    ownerUserId ?? "anonymous");
                return null;
            }

            _logger.LogDebug(
                "Conversation {ConversationId} not yet initialized, auto-initializing before streaming send",
                conversationId);
            await InitializeAsync(ownerUserId, string.Empty);
        }

        var userMessage = new ChatMessage(Guid.NewGuid(), "user", message.Content.Trim(), DateTime.UtcNow);
        var assistantMessageId = Guid.NewGuid();
        var ollamaMessages = _state.State.Messages
            .Select(m => new OllamaMessage(m.Role, m.Content))
            .Append(new OllamaMessage(userMessage.Role, userMessage.Content))
            .ToList();

        _pendingStreamMessage = new PendingStreamMessage(ownerUserId, assistantMessageId);

        _logger.LogDebug(
            "Prepared streaming message for conversation {ConversationId} using model '{Model}' ({MessageCount} messages in history)",
            conversationId,
            _state.State.Model,
            ollamaMessages.Count);

        return new StreamMessageStartResponse(
            conversationId,
            userMessage,
            CreateConversationSummary(conversationId),
            _state.State.Model,
            assistantMessageId,
            ollamaMessages.AsReadOnly());
    }

    public async Task<SendMessageResponse?> CompleteStreamMessageAsync(
        string? ownerUserId,
        ChatMessage userMessage,
        Guid assistantMessageId,
        string assistantContent,
        int latencyMs)
    {
        var conversationId = this.GetPrimaryKey();

        if (!await EnsureConversationLoadedAsync(ownerUserId))
        {
            _logger.LogDebug(
                "CompleteStreamMessage rejected for unavailable conversation {ConversationId} and user '{OwnerUserId}'",
                conversationId,
                ownerUserId ?? "anonymous");
            return null;
        }

        if (_pendingStreamMessage is not null
            && (_pendingStreamMessage.AssistantMessageId != assistantMessageId
                || !string.Equals(_pendingStreamMessage.OwnerUserId, ownerUserId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Streaming completion did not match the active conversation stream.");
        }

        var previousTitle = _state.State.Title;
        var previousUpdatedAtUtc = _state.State.UpdatedAtUtc;
        var previousMessageCount = _state.State.Messages.Count;

        try
        {
            _state.State.Messages.Add(userMessage);
            ApplyDefaultTitleFromFirstUserMessage(userMessage);

            var assistantMessage = new ChatMessage(assistantMessageId, "assistant", assistantContent, DateTime.UtcNow);
            _state.State.Messages.Add(assistantMessage);
            _state.State.UpdatedAtUtc = assistantMessage.CreatedAtUtc;

            if (_state.State.IsManagedConversation && !string.IsNullOrWhiteSpace(_state.State.OwnerUserId))
            {
                await _conversationStore.AppendMessagesAsync(
                    conversationId,
                    _state.State.OwnerUserId!,
                    _state.State.Title,
                    _state.State.HasManualTitle,
                    _state.State.UpdatedAtUtc,
                    [
                        ToStoredMessage(userMessage),
                        ToStoredMessage(assistantMessage)
                    ]);
            }

            await _state.WriteStateAsync();

            _logger.LogInformation(
                "Streaming response completed for conversation {ConversationId} with model '{Model}' in {LatencyMs}ms",
                conversationId,
                _state.State.Model,
                latencyMs);

            return new SendMessageResponse(
                conversationId,
                userMessage,
                assistantMessage,
                CreateConversationSummary(conversationId),
                _state.State.Model,
                "completed",
                latencyMs);
        }
        catch
        {
            _state.State.Messages.RemoveRange(previousMessageCount, _state.State.Messages.Count - previousMessageCount);
            _state.State.Title = previousTitle;
            _state.State.UpdatedAtUtc = previousUpdatedAtUtc;

            if (previousMessageCount == 0)
            {
                await RollbackFailedEmptyManagedConversationAsync(conversationId);
            }

            throw;
        }
        finally
        {
            ClearPendingStreamMessage(assistantMessageId);
        }
    }

    public async Task AbortStreamMessageAsync(string? ownerUserId, Guid assistantMessageId)
    {
        var conversationId = this.GetPrimaryKey();

        if (!await EnsureConversationLoadedAsync(ownerUserId))
        {
            ClearPendingStreamMessage(assistantMessageId);
            return;
        }

        _logger.LogDebug(
            "Aborting streaming response for conversation {ConversationId} and assistant message {AssistantMessageId}",
            conversationId,
            assistantMessageId);

        ClearPendingStreamMessage(assistantMessageId);

        if (_state.State.Messages.Count == 0)
        {
            await RollbackFailedEmptyManagedConversationAsync(conversationId);
        }
    }

    public async Task<ConversationHistoryOperationResult> GetHistoryAsync(string? ownerUserId)
    {
        var conversationId = this.GetPrimaryKey();
        var accessOutcome = await EnsureHistoryAccessAsync(ownerUserId);

        if (accessOutcome is not ConversationAccessOutcome.Success)
        {
            _logger.LogDebug(
                "GetHistory rejected for conversation {ConversationId} with outcome {Outcome}",
                conversationId,
                accessOutcome);
            return new ConversationHistoryOperationResult(accessOutcome, null);
        }

        return new ConversationHistoryOperationResult(
            ConversationAccessOutcome.Success,
            CreateHistoryResponse(conversationId));
    }

    public async Task<RenameConversationOperationResult> RenameAsync(string ownerUserId, string title)
    {
        var conversationId = this.GetPrimaryKey();
        var accessOutcome = await EnsureManagedConversationAccessAsync(ownerUserId);
        if (accessOutcome is not ConversationAccessOutcome.Success)
        {
            _logger.LogDebug(
                "Rename rejected for conversation {ConversationId} and user '{OwnerUserId}' with outcome {Outcome}",
                conversationId,
                ownerUserId,
                accessOutcome);
            return new RenameConversationOperationResult(accessOutcome, null);
        }

        var normalizedTitle = title.Trim();
        var updatedAtUtc = DateTime.UtcNow;

        _state.State.Title = normalizedTitle;
        _state.State.HasManualTitle = true;
        _state.State.UpdatedAtUtc = updatedAtUtc;

        await _conversationStore.RenameConversationAsync(
            conversationId,
            ownerUserId,
            normalizedTitle,
            updatedAtUtc);

        await _state.WriteStateAsync();

        return new RenameConversationOperationResult(
            ConversationAccessOutcome.Success,
            CreateConversationSummary(conversationId));
    }

    public async Task<DeleteConversationOperationResult> DeleteAsync(string ownerUserId)
    {
        var conversationId = this.GetPrimaryKey();
        var accessOutcome = await EnsureManagedConversationAccessAsync(ownerUserId);
        if (accessOutcome is not ConversationAccessOutcome.Success)
        {
            _logger.LogDebug(
                "Delete rejected for conversation {ConversationId} and user '{OwnerUserId}' with outcome {Outcome}",
                conversationId,
                ownerUserId,
                accessOutcome);
            return new DeleteConversationOperationResult(accessOutcome);
        }

        await _conversationStore.DeleteConversationAsync(conversationId, ownerUserId);
        ResetState();
        await _state.ClearStateAsync();
        DeactivateOnIdle();

        return new DeleteConversationOperationResult(ConversationAccessOutcome.Success);
    }

    private async Task<bool> EnsureConversationLoadedAsync(string? ownerUserId)
    {
        if (_state.State.IsInitialized && HasAccess(ownerUserId))
        {
            return true;
        }

        var conversationId = this.GetPrimaryKey();
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            if (await _conversationStore.ExistsAsync(conversationId))
            {
                return false;
            }

            return false;
        }

        var persistedConversation = await _conversationStore.GetHistoryAsync(conversationId, ownerUserId);
        if (persistedConversation is null)
        {
            return false;
        }

        HydrateFromPersistedConversation(persistedConversation);
        await _state.WriteStateAsync();
        return true;
    }

    private async Task<ConversationAccessOutcome> EnsureHistoryAccessAsync(string? ownerUserId)
    {
        if (_state.State.IsInitialized)
        {
            return EvaluateHistoryAccess(ownerUserId);
        }

        var conversationId = this.GetPrimaryKey();
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            return await _conversationStore.ExistsAsync(conversationId)
                ? ConversationAccessOutcome.AuthenticationRequired
                : ConversationAccessOutcome.NotFound;
        }

        var persistedConversation = await _conversationStore.GetHistoryAsync(conversationId, ownerUserId);
        if (persistedConversation is not null)
        {
            HydrateFromPersistedConversation(persistedConversation);
            await _state.WriteStateAsync();
            return ConversationAccessOutcome.Success;
        }

        return await _conversationStore.ExistsAsync(conversationId)
            ? ConversationAccessOutcome.Forbidden
            : ConversationAccessOutcome.NotFound;
    }

    private async Task<ConversationAccessOutcome> EnsureManagedConversationAccessAsync(string ownerUserId)
    {
        if (_state.State.IsInitialized)
        {
            return EvaluateManagedConversationAccess(ownerUserId);
        }

        var conversationId = this.GetPrimaryKey();
        var persistedConversation = await _conversationStore.GetHistoryAsync(conversationId, ownerUserId);
        if (persistedConversation is not null)
        {
            HydrateFromPersistedConversation(persistedConversation);
            await _state.WriteStateAsync();
            return ConversationAccessOutcome.Success;
        }

        return await _conversationStore.ExistsAsync(conversationId)
            ? ConversationAccessOutcome.Forbidden
            : ConversationAccessOutcome.NotFound;
    }

    private bool HasAccess(string? ownerUserId)
    {
        if (!_state.State.IsInitialized)
        {
            return false;
        }

        if (!_state.State.IsManagedConversation)
        {
            return string.IsNullOrWhiteSpace(ownerUserId);
        }

        return string.Equals(_state.State.OwnerUserId, ownerUserId, StringComparison.Ordinal);
    }

    private ConversationAccessOutcome EvaluateHistoryAccess(string? ownerUserId)
    {
        if (!_state.State.IsManagedConversation)
        {
            return string.IsNullOrWhiteSpace(ownerUserId)
                ? ConversationAccessOutcome.Success
                : ConversationAccessOutcome.NotFound;
        }

        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            return ConversationAccessOutcome.AuthenticationRequired;
        }

        return string.Equals(_state.State.OwnerUserId, ownerUserId, StringComparison.Ordinal)
            ? ConversationAccessOutcome.Success
            : ConversationAccessOutcome.Forbidden;
    }

    private ConversationAccessOutcome EvaluateManagedConversationAccess(string ownerUserId)
    {
        if (!_state.State.IsManagedConversation)
        {
            return ConversationAccessOutcome.NotFound;
        }

        return string.Equals(_state.State.OwnerUserId, ownerUserId, StringComparison.Ordinal)
            ? ConversationAccessOutcome.Success
            : ConversationAccessOutcome.Forbidden;
    }

    private void ApplyDefaultTitleFromFirstUserMessage(ChatMessage userMessage)
    {
        if (_state.State.HasManualTitle)
        {
            return;
        }

        var userMessageCount = _state.State.Messages.Count(static message => message.Role == "user");
        if (userMessageCount != 1)
        {
            return;
        }

        _state.State.Title = ConversationTitleGenerator.CreateDefaultTitleFromFirstMessage(userMessage.Content);
    }

    private void EnsureNoPendingStream()
    {
        if (_pendingStreamMessage is not null)
        {
            throw new InvalidOperationException("Conversation is already processing a streamed reply.");
        }
    }

    private void ClearPendingStreamMessage(Guid assistantMessageId)
    {
        if (_pendingStreamMessage?.AssistantMessageId == assistantMessageId)
        {
            _pendingStreamMessage = null;
        }
    }

    private async Task RollbackFailedEmptyManagedConversationAsync(Guid conversationId)
    {
        if (!_state.State.IsManagedConversation || string.IsNullOrWhiteSpace(_state.State.OwnerUserId))
        {
            return;
        }

        await _conversationStore.DeleteConversationAsync(conversationId, _state.State.OwnerUserId!);
        ResetState();
        await _state.ClearStateAsync();
        DeactivateOnIdle();
    }

    private void HydrateFromPersistedConversation(UserOwnedConversationHistory persistedConversation)
    {
        _state.State.Title = persistedConversation.Summary.Title;
        _state.State.Model = persistedConversation.Summary.Model;
        _state.State.Messages = persistedConversation.Messages
            .OrderBy(message => message.Sequence)
            .Select(message => new ChatMessage(
                message.MessageId,
                message.Role,
                message.Content,
                message.CreatedAtUtc))
            .ToList();
        _state.State.CreatedAtUtc = persistedConversation.Summary.CreatedAtUtc;
        _state.State.UpdatedAtUtc = persistedConversation.Summary.UpdatedAtUtc;
        _state.State.IsInitialized = true;
        _state.State.OwnerUserId = persistedConversation.Summary.OwnerUserId;
        _state.State.IsManagedConversation = true;
        _state.State.HasManualTitle = persistedConversation.Summary.HasManualTitle;
    }

    private void ResetState()
    {
        _state.State.Title = string.Empty;
        _state.State.Model = string.Empty;
        _state.State.Messages = [];
        _state.State.CreatedAtUtc = default;
        _state.State.UpdatedAtUtc = default;
        _state.State.IsInitialized = false;
        _state.State.OwnerUserId = null;
        _state.State.IsManagedConversation = false;
        _state.State.HasManualTitle = false;
    }

    private static StoredConversationMessage ToStoredMessage(ChatMessage message) =>
        new(message.MessageId, message.Role, message.Content, message.CreatedAtUtc, 0);

    private GetConversationHistoryResponse CreateHistoryResponse(Guid conversationId) =>
        new(
            conversationId,
            _state.State.Title,
            _state.State.HasManualTitle,
            _state.State.Model,
            _state.State.CreatedAtUtc,
            _state.State.UpdatedAtUtc,
            "active",
            _state.State.Messages.AsReadOnly());

    private ConversationSummary CreateConversationSummary(Guid conversationId) =>
        new(
            conversationId,
            _state.State.Title,
            _state.State.HasManualTitle,
            _state.State.Model,
            _state.State.CreatedAtUtc,
            _state.State.UpdatedAtUtc,
            "active");

    private CreateConversationResponse CreateConversationResponse(Guid conversationId) =>
        new(
            conversationId,
            _state.State.Title,
            _state.State.HasManualTitle,
            _state.State.Model,
            _state.State.CreatedAtUtc,
            _state.State.UpdatedAtUtc,
            "active");
}
