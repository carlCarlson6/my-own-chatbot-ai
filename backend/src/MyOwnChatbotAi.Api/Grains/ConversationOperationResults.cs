using MyOwnChatbotAi.Api.Contracts;
using MyOwnChatbotAi.Api.Services.Ollama;

namespace MyOwnChatbotAi.Api.Grains;

[GenerateSerializer]
public enum ConversationAccessOutcome
{
    Success = 0,
    NotFound = 1,
    AuthenticationRequired = 2,
    Forbidden = 3
}

[GenerateSerializer]
public sealed record ConversationHistoryOperationResult(
    [property: Id(0)] ConversationAccessOutcome Outcome,
    [property: Id(1)] GetConversationHistoryResponse? Response);

[GenerateSerializer]
public sealed record StreamMessageStartResponse(
    [property: Id(0)] Guid ConversationId,
    [property: Id(1)] ChatMessage UserMessage,
    [property: Id(2)] ConversationSummary Conversation,
    [property: Id(3)] string Model,
    [property: Id(4)] Guid AssistantMessageId,
    [property: Id(5)] IReadOnlyList<OllamaMessage> OllamaMessages);

[GenerateSerializer]
public sealed record RenameConversationOperationResult(
    [property: Id(0)] ConversationAccessOutcome Outcome,
    [property: Id(1)] ConversationSummary? Conversation);

[GenerateSerializer]
public sealed record DeleteConversationOperationResult(
    [property: Id(0)] ConversationAccessOutcome Outcome);
