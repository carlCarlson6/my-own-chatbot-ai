namespace MyOwnChatbotAi.Api.Features.Conversations.Persistence;

public interface IUserOwnedConversationStore
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid conversationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserOwnedConversationSummary>> ListAsync(
        string ownerUserId,
        CancellationToken cancellationToken = default);

    Task<UserOwnedConversationHistory?> GetHistoryAsync(
        Guid conversationId,
        string ownerUserId,
        CancellationToken cancellationToken = default);

    Task CreateConversationAsync(
        UserOwnedConversationSummary conversation,
        CancellationToken cancellationToken = default);

    Task AppendMessagesAsync(
        Guid conversationId,
        string ownerUserId,
        string title,
        bool hasManualTitle,
        DateTime updatedAtUtc,
        IReadOnlyList<StoredConversationMessage> messages,
        CancellationToken cancellationToken = default);

    Task RenameConversationAsync(
        Guid conversationId,
        string ownerUserId,
        string title,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken = default);

    Task DeleteConversationAsync(
        Guid conversationId,
        string ownerUserId,
        CancellationToken cancellationToken = default);
}
