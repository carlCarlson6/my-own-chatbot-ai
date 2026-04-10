using MyOwnChatbotAi.Api.Contracts;
using MyOwnChatbotAi.Api.Services;

namespace MyOwnChatbotAi.Api.Features.Conversations;

public static class CreateConversationEndpoint
{
    public static RouteGroupBuilder MapCreateConversationEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost(
            string.Empty,
            (CreateConversationRequest? request, IConversationService conversations) =>
            {
                var response = conversations.CreateConversation(request);
                return Results.Created($"/api/conversations/{response.ConversationId}/history", response);
            })
            .WithName("CreateConversation")
            .Produces<CreateConversationResponse>(StatusCodes.Status201Created)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        return group;
    }
}
