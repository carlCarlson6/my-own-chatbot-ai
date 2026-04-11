namespace MyOwnChatbotAi.Api.Features.Conversations.Persistence;

public sealed record StoredConversationMessage(
    Guid MessageId,
    string Role,
    string Content,
    DateTime CreatedAtUtc,
    int Sequence);

public sealed record UserOwnedConversationSummary(
    Guid ConversationId,
    string OwnerUserId,
    string Title,
    bool HasManualTitle,
    string Model,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string Status);

public sealed record UserOwnedConversationHistory(
    UserOwnedConversationSummary Summary,
    IReadOnlyList<StoredConversationMessage> Messages);
