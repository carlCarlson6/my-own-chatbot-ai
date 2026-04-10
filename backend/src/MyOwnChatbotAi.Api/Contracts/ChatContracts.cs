namespace MyOwnChatbotAi.Api.Contracts;

public sealed record CreateConversationRequest(string? Title, string? Model);

public sealed record CreateConversationResponse(
    Guid ConversationId,
    string Title,
    string Model,
    DateTime CreatedAtUtc,
    string Status);

public sealed record ChatMessageInput(string Content, string? ClientMessageId);

public sealed record SendMessageRequest(Guid? ConversationId, string? Model, ChatMessageInput Message);

public sealed record ChatMessage(
    Guid MessageId,
    string Role,
    string Content,
    DateTime CreatedAtUtc);

public sealed record SendMessageResponse(
    Guid ConversationId,
    ChatMessage UserMessage,
    ChatMessage AssistantMessage,
    string Model,
    string Status,
    int? LatencyMs);

public sealed record GetConversationHistoryResponse(
    Guid ConversationId,
    string Title,
    string Model,
    string Status,
    IReadOnlyList<ChatMessage> Messages);

public sealed record ModelSummary(string Name, string DisplayName, bool IsDefault, string? Description);

public sealed record ListModelsResponse(IReadOnlyList<ModelSummary> Models);

public sealed record ApiError(
    string Code,
    string Message,
    string? Target = null,
    IReadOnlyList<string>? Details = null);
