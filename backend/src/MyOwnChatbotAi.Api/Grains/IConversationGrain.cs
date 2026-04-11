using MyOwnChatbotAi.Api.Contracts;

namespace MyOwnChatbotAi.Api.Grains;

public interface IConversationGrain : IGrainWithGuidKey
{
    Task<CreateConversationResponse> InitializeAsync(string? ownerUserId, string title);

    Task<SendMessageResponse?> SendMessageAsync(string? ownerUserId, ChatMessageInput message, bool createIfMissing);
    Task<StreamMessageStartResponse?> BeginStreamMessageAsync(string? ownerUserId, ChatMessageInput message, bool createIfMissing);
    Task<SendMessageResponse?> CompleteStreamMessageAsync(
        string? ownerUserId,
        ChatMessage userMessage,
        Guid assistantMessageId,
        string assistantContent,
        int latencyMs);
    Task AbortStreamMessageAsync(string? ownerUserId, Guid assistantMessageId);

    Task<ConversationHistoryOperationResult> GetHistoryAsync(string? ownerUserId);

    Task<RenameConversationOperationResult> RenameAsync(string ownerUserId, string title);

    Task<DeleteConversationOperationResult> DeleteAsync(string ownerUserId);
}
