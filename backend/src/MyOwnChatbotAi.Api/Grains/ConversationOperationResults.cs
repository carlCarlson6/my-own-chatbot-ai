using MyOwnChatbotAi.Api.Contracts;

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
public sealed record RenameConversationOperationResult(
    [property: Id(0)] ConversationAccessOutcome Outcome,
    [property: Id(1)] ConversationSummary? Conversation);

[GenerateSerializer]
public sealed record DeleteConversationOperationResult(
    [property: Id(0)] ConversationAccessOutcome Outcome);
