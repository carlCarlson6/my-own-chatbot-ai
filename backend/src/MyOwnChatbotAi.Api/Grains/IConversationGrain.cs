using MyOwnChatbotAi.Api.Contracts;

namespace MyOwnChatbotAi.Api.Grains;

public interface IConversationGrain : IGrainWithGuidKey
{
    Task<CreateConversationResponse> InitializeAsync(string? ownerUserId, string title);

    Task<SendMessageResponse?> SendMessageAsync(string? ownerUserId, ChatMessageInput message, bool createIfMissing);

    Task<ConversationHistoryOperationResult> GetHistoryAsync(string? ownerUserId);

    Task<RenameConversationOperationResult> RenameAsync(string ownerUserId, string title);

    Task<DeleteConversationOperationResult> DeleteAsync(string ownerUserId);
}
