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
    private readonly IPersistentState<ConversationState> _state;
    private readonly IUserOwnedConversationStore _conversationStore;
    private readonly IOllamaClient _ollamaClient;
    private readonly OllamaOptions _options;
    private readonly ILogger<ConversationGrain> _logger;

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
        string assistantContent;
        try
        {
            assistantContent = await _ollamaClient.ChatAsync(_state.State.Model, ollamaMessages);
        }
        catch (OllamaException ex)
        {
            _state.State.Messages.Remove(userMessage);
            _state.State.Title = previousTitle;
            _state.State.UpdatedAtUtc = previousUpdatedAtUtc;
            _logger.LogError(ex,
                "Ollama failed to generate a reply for conversation {ConversationId} with model '{Model}'",
                conversationId, _state.State.Model);
            throw new InvalidOperationException(
                $"Ollama failed to generate a reply for model '{_state.State.Model}': {ex.Message}", ex);
        }

        var latencyMs = (int)(DateTime.UtcNow - startedAt).TotalMilliseconds;
        _logger.LogInformation(
            "Ollama responded for conversation {ConversationId} with model '{Model}' in {LatencyMs}ms",
            conversationId, _state.State.Model, latencyMs);

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
            _state.State.Model,
            "completed",
            latencyMs);
    }

    public async Task<GetConversationHistoryResponse?> GetHistoryAsync(string? ownerUserId)
    {
        var conversationId = this.GetPrimaryKey();

        if (!await EnsureConversationLoadedAsync(ownerUserId))
        {
            _logger.LogDebug("GetHistory requested for uninitialized conversation {ConversationId}", conversationId);
            return null;
        }

        return new GetConversationHistoryResponse(
            conversationId,
            _state.State.Title,
            _state.State.Model,
            "active",
            _state.State.Messages.AsReadOnly());
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

    private CreateConversationResponse CreateConversationResponse(Guid conversationId) =>
        new(
            conversationId,
            _state.State.Title,
            _state.State.Model,
            _state.State.CreatedAtUtc,
            "active");
}
