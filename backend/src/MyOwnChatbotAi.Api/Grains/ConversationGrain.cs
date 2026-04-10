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

    public ConversationGrain(
        [PersistentState("state", "conversations")] IPersistentState<ConversationState> state,
        IOllamaClient ollamaClient,
        IOptions<OllamaOptions> options)
    {
        _state = state;
        _ollamaClient = ollamaClient;
        _options = options.Value;
    }

    public async Task<CreateConversationResponse> InitializeAsync(string title, string model)
    {
        if (_state.State.IsInitialized)
        {
            return new CreateConversationResponse(
                this.GetPrimaryKey(),
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

        return new CreateConversationResponse(
            this.GetPrimaryKey(),
            _state.State.Title,
            _state.State.Model,
            _state.State.CreatedAtUtc,
            "active");
    }

    public async Task<SendMessageResponse> SendMessageAsync(
        ChatMessageInput message,
        string? model)
    {
        if (!_state.State.IsInitialized)
        {
            await InitializeAsync("New conversation", model ?? _options.DefaultModel);
        }
        else if (!string.IsNullOrWhiteSpace(model))
        {
            _state.State.Model = NormalizeModel(model);
        }

        var userMessage = new ChatMessage(Guid.NewGuid(), "user", message.Content.Trim(), DateTime.UtcNow);
        _state.State.Messages.Add(userMessage);

        var ollamaMessages = _state.State.Messages
            .Select(m => new OllamaMessage(m.Role, m.Content))
            .ToList();

        var startedAt = DateTime.UtcNow;
        string assistantContent;
        try
        {
            assistantContent = await _ollamaClient.ChatAsync(_state.State.Model, ollamaMessages);
        }
        catch (OllamaException ex)
        {
            _state.State.Messages.Remove(userMessage);
            throw new InvalidOperationException(
                $"Ollama failed to generate a reply for model '{_state.State.Model}': {ex.Message}", ex);
        }

        var latencyMs = (int)(DateTime.UtcNow - startedAt).TotalMilliseconds;
        var assistantMessage = new ChatMessage(Guid.NewGuid(), "assistant", assistantContent, DateTime.UtcNow);
        _state.State.Messages.Add(assistantMessage);

        await _state.WriteStateAsync();

        return new SendMessageResponse(
            this.GetPrimaryKey(),
            userMessage,
            assistantMessage,
            _state.State.Model,
            "completed",
            latencyMs);
    }

    public Task<GetConversationHistoryResponse?> GetHistoryAsync()
    {
        if (!_state.State.IsInitialized)
        {
            return Task.FromResult<GetConversationHistoryResponse?>(null);
        }

        return Task.FromResult<GetConversationHistoryResponse?>(
            new GetConversationHistoryResponse(
                this.GetPrimaryKey(),
                _state.State.Title,
                _state.State.Model,
                "active",
                _state.State.Messages.AsReadOnly()));
    }

    private string NormalizeModel(string? model) =>
        string.IsNullOrWhiteSpace(model) ? _options.DefaultModel : model.Trim();
}
