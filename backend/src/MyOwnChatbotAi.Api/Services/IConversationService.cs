using MyOwnChatbotAi.Api.Contracts;

namespace MyOwnChatbotAi.Api.Services;

public interface IConversationService
{
    CreateConversationResponse CreateConversation(CreateConversationRequest? request);

    SendMessageResponse SendMessage(SendMessageRequest request);

    GetConversationHistoryResponse? GetHistory(Guid conversationId);

    ListModelsResponse GetModels();
}
