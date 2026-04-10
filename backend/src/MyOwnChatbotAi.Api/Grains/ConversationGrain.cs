using Microsoft.Extensions.Options;
using MyOwnChatbotAi.Api.Contracts;
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
}

public sealed class ConversationGrain : Grain, IConversationGrain
{
    private readonly IPersistentState<ConversationState> _state;
    private readonly IOllamaClient _ollamaClient;
    private readonly OllamaOptions _options;
    private readonly ILogger<ConversationGrain> _logger;

    public ConversationGrain(
        [PersistentState("state", "conversations")] IPersistentState<ConversationState> state,
        IOllamaClient ollamaClient,
        IOptions<OllamaOptions> options,
        ILogger<ConversationGrain> logger)
    {
        _state = state;
        _ollamaClient = ollamaClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CreateConversationResponse> InitializeAsync(string title, string model)
    {
        var conversationId = this.GetPrimaryKey();

        if (_state.State.IsInitialized)
        {
            _logger.LogDebug("Conversation {ConversationId} already initialized, returning existing state", conversationId);
            return new CreateConversationResponse(
                conversationId,
                _state.State.Title,
                _state.State.Model,
                _state.State.CreatedAtUtc,
                "active");
        }

        var normalizedModel = NormalizeModel(model);
        var normalizedTitle = string.IsNullOrWhiteSpace(title) ? "New conversation" : title.Trim();

        _state.State.Title = normalizedTitle;
        _state.State.Model = normalizedModel;
        _state.State.CreatedAtUtc = DateTime.UtcNow;
        _state.State.IsInitialized = true;

        await _state.WriteStateAsync();

        _logger.LogInformation(
            "Conversation {ConversationId} initialized with model '{Model}' and title '{Title}'",
            conversationId, normalizedModel, normalizedTitle);

        return new CreateConversationResponse(
            conversationId,
            _state.State.Title,
            _state.State.Model,
            _state.State.CreatedAtUtc,
            "active");
    }

    public async Task<SendMessageResponse> SendMessageAsync(ChatMessageInput message, string? model)
    {
        var conversationId = this.GetPrimaryKey();

        if (!_state.State.IsInitialized)
        {
            _logger.LogDebug("Conversation {ConversationId} not yet initialized, auto-initializing before send", conversationId);
            await InitializeAsync("New conversation", model ?? _options.DefaultModel);
        }
        else if (!string.IsNullOrWhiteSpace(model))
        {
            var previousModel = _state.State.Model;
            _state.State.Model = NormalizeModel(model);
            _logger.LogDebug(
                "Model override for conversation {ConversationId}: '{PreviousModel}' -> '{Model}'",
                conversationId, previousModel, _state.State.Model);
        }

        var userMessage = new ChatMessage(Guid.NewGuid(), "user", message.Content.Trim(), DateTime.UtcNow);
        _state.State.Messages.Add(userMessage);

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

        await _state.WriteStateAsync();

        return new SendMessageResponse(
            conversationId,
            userMessage,
            assistantMessage,
            _state.State.Model,
            "completed",
            latencyMs);
    }

    public Task<GetConversationHistoryResponse?> GetHistoryAsync()
    {
        var conversationId = this.GetPrimaryKey();

        if (!_state.State.IsInitialized)
        {
            _logger.LogDebug("GetHistory requested for uninitialized conversation {ConversationId}", conversationId);
            return Task.FromResult<GetConversationHistoryResponse?>(null);
        }

        return Task.FromResult<GetConversationHistoryResponse?>(
            new GetConversationHistoryResponse(
                conversationId,
                _state.State.Title,
                _state.State.Model,
                "active",
                _state.State.Messages.AsReadOnly()));
    }

    private string NormalizeModel(string? model) =>
        string.IsNullOrWhiteSpace(model) ? _options.DefaultModel : model.Trim();
}
