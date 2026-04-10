using MyOwnChatbotAi.Api.Contracts;
using MyOwnChatbotAi.Api.Services;

namespace MyOwnChatbotAi.Api.Features.Conversations;

public static class GetConversationHistoryEndpoint
{
    public static RouteGroupBuilder MapGetConversationHistoryEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet(
            "/{conversationId:guid}/history",
            (Guid conversationId, IConversationService conversations) =>
            {
                var response = conversations.GetHistory(conversationId);

                return response is null
                    ? Results.NotFound(new ApiError("conversation_not_found", $"Conversation '{conversationId}' was not found.", "conversationId"))
                    : Results.Ok(response);
            })
            .WithName("GetConversationHistory")
            .Produces<GetConversationHistoryResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status404NotFound)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        return group;
    }
}
