namespace MyOwnChatbotAi.Api.Contracts;

[GenerateSerializer]
public sealed record CreateConversationRequest(
    [property: Id(0)] string? Title,
    [property: Id(1)] string? Model);

[GenerateSerializer]
public sealed record CreateConversationResponse(
    [property: Id(0)] Guid ConversationId,
    [property: Id(1)] string Title,
    [property: Id(2)] string Model,
    [property: Id(3)] DateTime CreatedAtUtc,
    [property: Id(4)] string Status);

[GenerateSerializer]
public sealed record ChatMessageInput(
    [property: Id(0)] string Content,
    [property: Id(1)] string? ClientMessageId);

[GenerateSerializer]
public sealed record SendMessageRequest(
    [property: Id(0)] Guid? ConversationId,
    [property: Id(1)] string? Model,
    [property: Id(2)] ChatMessageInput Message);

[GenerateSerializer]
public sealed record ChatMessage(
    [property: Id(0)] Guid MessageId,
    [property: Id(1)] string Role,
    [property: Id(2)] string Content,
    [property: Id(3)] DateTime CreatedAtUtc);

[GenerateSerializer]
public sealed record SendMessageResponse(
    [property: Id(0)] Guid ConversationId,
    [property: Id(1)] ChatMessage UserMessage,
    [property: Id(2)] ChatMessage AssistantMessage,
    [property: Id(3)] string Model,
    [property: Id(4)] string Status,
    [property: Id(5)] int? LatencyMs);

[GenerateSerializer]
public sealed record GetConversationHistoryResponse(
    [property: Id(0)] Guid ConversationId,
    [property: Id(1)] string Title,
    [property: Id(2)] string Model,
    [property: Id(3)] string Status,
    [property: Id(4)] IReadOnlyList<ChatMessage> Messages);

[GenerateSerializer]
public sealed record ModelSummary(
    [property: Id(0)] string Name,
    [property: Id(1)] string DisplayName,
    [property: Id(2)] bool IsDefault,
    [property: Id(3)] string? Description);

[GenerateSerializer]
public sealed record ListModelsResponse(
    [property: Id(0)] IReadOnlyList<ModelSummary> Models);

[GenerateSerializer]
public sealed record ApiError(
    [property: Id(0)] string Code,
    [property: Id(1)] string Message,
    [property: Id(2)] string? Target = null,
    [property: Id(3)] IReadOnlyList<string>? Details = null);
