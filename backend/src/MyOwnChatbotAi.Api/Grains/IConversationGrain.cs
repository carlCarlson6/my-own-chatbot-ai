using MyOwnChatbotAi.Api.Contracts;

namespace MyOwnChatbotAi.Api.Grains;

public interface IConversationGrain : IGrainWithGuidKey
{
    Task<CreateConversationResponse> InitializeAsync(string title);

    Task<SendMessageResponse> SendMessageAsync(ChatMessageInput message);

    Task<GetConversationHistoryResponse?> GetHistoryAsync();
}
