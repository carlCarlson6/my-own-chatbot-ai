using MyOwnChatbotAi.Api.Contracts;

namespace MyOwnChatbotAi.Api.Grains;

public interface IConversationGrain : IGrainWithGuidKey
{
    Task<CreateConversationResponse> InitializeAsync(string? ownerUserId, string title);

    Task<SendMessageResponse?> SendMessageAsync(string? ownerUserId, ChatMessageInput message, bool createIfMissing);

    Task<GetConversationHistoryResponse?> GetHistoryAsync(string? ownerUserId);
}
