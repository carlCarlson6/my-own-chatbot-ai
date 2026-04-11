namespace MyOwnChatbotAi.Api.Contracts;

[GenerateSerializer]
public sealed record CreateConversationRequest(
    [property: Id(0)] string? Title);

[GenerateSerializer]
public sealed record CreateConversationResponse(
    [property: Id(0)] Guid ConversationId,
    [property: Id(1)] string Title,
    [property: Id(2)] bool HasManualTitle,
    [property: Id(3)] string Model,
    [property: Id(4)] DateTime CreatedAtUtc,
    [property: Id(5)] DateTime UpdatedAtUtc,
    [property: Id(6)] string Status);

[GenerateSerializer]
public sealed record ChatMessageInput(
    [property: Id(0)] string Content,
    [property: Id(1)] string? ClientMessageId);

[GenerateSerializer]
public sealed record SendMessageRequest(
    [property: Id(0)] Guid? ConversationId,
    [property: Id(1)] ChatMessageInput Message);

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
    [property: Id(3)] ConversationSummary Conversation,
    [property: Id(4)] string Model,
    [property: Id(5)] string Status,
    [property: Id(6)] int? LatencyMs);

[GenerateSerializer]
public sealed record ConversationStreamStartedEvent(
    [property: Id(0)] string Type,
    [property: Id(1)] Guid ConversationId,
    [property: Id(2)] ChatMessage UserMessage,
    [property: Id(3)] ConversationSummary Conversation,
    [property: Id(4)] string Model,
    [property: Id(5)] Guid AssistantMessageId);

[GenerateSerializer]
public sealed record ConversationStreamChunkEvent(
    [property: Id(0)] string Type,
    [property: Id(1)] Guid ConversationId,
    [property: Id(2)] Guid AssistantMessageId,
    [property: Id(3)] string Delta,
    [property: Id(4)] int Sequence);

[GenerateSerializer]
public sealed record ConversationStreamCompletedEvent(
    [property: Id(0)] string Type,
    [property: Id(1)] Guid ConversationId,
    [property: Id(2)] ChatMessage UserMessage,
    [property: Id(3)] ChatMessage AssistantMessage,
    [property: Id(4)] ConversationSummary Conversation,
    [property: Id(5)] string Model,
    [property: Id(6)] string Status,
    [property: Id(7)] int? LatencyMs);

[GenerateSerializer]
public sealed record ConversationStreamErrorEvent(
    [property: Id(0)] string Type,
    [property: Id(1)] Guid ConversationId,
    [property: Id(2)] Guid AssistantMessageId,
    [property: Id(3)] string Code,
    [property: Id(4)] string Message,
    [property: Id(5)] string? Target = null,
    [property: Id(6)] IReadOnlyList<string>? Details = null);

[GenerateSerializer]
public sealed record GetConversationHistoryResponse(
    [property: Id(0)] Guid ConversationId,
    [property: Id(1)] string Title,
    [property: Id(2)] bool HasManualTitle,
    [property: Id(3)] string Model,
    [property: Id(4)] DateTime CreatedAtUtc,
    [property: Id(5)] DateTime UpdatedAtUtc,
    [property: Id(6)] string Status,
    [property: Id(7)] IReadOnlyList<ChatMessage> Messages);

[GenerateSerializer]
public sealed record ConversationSummary(
    [property: Id(0)] Guid ConversationId,
    [property: Id(1)] string Title,
    [property: Id(2)] bool HasManualTitle,
    [property: Id(3)] string Model,
    [property: Id(4)] DateTime CreatedAtUtc,
    [property: Id(5)] DateTime UpdatedAtUtc,
    [property: Id(6)] string Status);

[GenerateSerializer]
public sealed record RenameConversationRequest(
    [property: Id(0)] string Title);

[GenerateSerializer]
public sealed record ApiError(
    [property: Id(0)] string Code,
    [property: Id(1)] string Message,
    [property: Id(2)] string? Target = null,
    [property: Id(3)] IReadOnlyList<string>? Details = null);
