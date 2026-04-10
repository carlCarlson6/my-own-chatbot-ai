using System.Collections.Concurrent;
using MyOwnChatbotAi.Api.Contracts;

namespace MyOwnChatbotAi.Api.Services;

public sealed class InMemoryConversationService : IConversationService
{
    private const string DefaultModel = "llama3.1";

    private readonly ConcurrentDictionary<Guid, ConversationState> _conversations = new();

    public CreateConversationResponse CreateConversation(CreateConversationRequest? request)
    {
        var conversationId = Guid.NewGuid();
        var createdAtUtc = DateTime.UtcNow;
        var title = string.IsNullOrWhiteSpace(request?.Title) ? "New conversation" : request!.Title!.Trim();
        var model = NormalizeModel(request?.Model);

        var state = new ConversationState(conversationId, title, model);
        _conversations[conversationId] = state;

        return new CreateConversationResponse(conversationId, title, model, createdAtUtc, "active");
    }

    public SendMessageResponse SendMessage(SendMessageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Message is null || string.IsNullOrWhiteSpace(request.Message.Content))
        {
            throw new ArgumentException("Message content is required.", nameof(request));
        }

        var state = ResolveConversation(request.ConversationId, request.Model);
        var now = DateTime.UtcNow;
        var userMessage = new ChatMessage(Guid.NewGuid(), "user", request.Message.Content.Trim(), now);
        var assistantMessage = new ChatMessage(
            Guid.NewGuid(),
            "assistant",
            $"Stub reply from {state.Model}: I received '{request.Message.Content.Trim()}'. Orleans and Ollama will be wired in next.",
            DateTime.UtcNow);

        lock (state.SyncRoot)
        {
            state.Messages.Add(userMessage);
            state.Messages.Add(assistantMessage);
        }

        return new SendMessageResponse(state.ConversationId, userMessage, assistantMessage, state.Model, "completed", 0);
    }

    public GetConversationHistoryResponse? GetHistory(Guid conversationId)
    {
        if (!_conversations.TryGetValue(conversationId, out var state))
        {
            return null;
        }

        lock (state.SyncRoot)
        {
            return new GetConversationHistoryResponse(
                state.ConversationId,
                state.Title,
                state.Model,
                "active",
                state.Messages.ToArray());
        }
    }

    public ListModelsResponse GetModels() =>
        new([
            new ModelSummary("llama3.1", "Llama 3.1", true, "Default local development model."),
            new ModelSummary("mistral", "Mistral", false, "Optional lightweight local model.")
        ]);

    private ConversationState ResolveConversation(Guid? conversationId, string? requestedModel)
    {
        if (conversationId is { } existingId)
        {
            if (_conversations.TryGetValue(existingId, out var existingState))
            {
                if (!string.IsNullOrWhiteSpace(requestedModel))
                {
                    existingState.Model = NormalizeModel(requestedModel);
                }

                return existingState;
            }

            throw new KeyNotFoundException($"Conversation '{existingId}' was not found.");
        }

        var created = CreateConversation(new CreateConversationRequest("New conversation", requestedModel));
        return _conversations[created.ConversationId];
    }

    private static string NormalizeModel(string? model) =>
        string.IsNullOrWhiteSpace(model) ? DefaultModel : model.Trim();

    private sealed class ConversationState(Guid conversationId, string title, string model)
    {
        public Guid ConversationId { get; } = conversationId;
        public string Title { get; } = title;
        public string Model { get; set; } = model;
        public List<ChatMessage> Messages { get; } = [];
        public object SyncRoot { get; } = new();
    }
}
