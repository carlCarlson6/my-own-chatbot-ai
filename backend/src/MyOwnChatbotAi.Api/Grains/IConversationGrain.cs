using MyOwnChatbotAi.Api.Contracts;

namespace MyOwnChatbotAi.Api.Grains;

public interface IConversationGrain : IGrainWithGuidKey
{
    Task<CreateConversationResponse> InitializeAsync(string title, string model);

    Task<SendMessageResponse> SendMessageAsync(ChatMessageInput message, string? model);

    Task<GetConversationHistoryResponse?> GetHistoryAsync();
}
