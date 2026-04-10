using MyOwnChatbotAi.Api.Contracts;
using MyOwnChatbotAi.Api.Grains;

namespace MyOwnChatbotAi.Api.Features.Conversations;

public static class CreateConversationEndpoint
{
    private const string Route = "";

    public static RouteGroupBuilder MapCreateConversationEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost(Route, Handle)
            .WithName("CreateConversation")
            .Produces<CreateConversationResponse>(StatusCodes.Status201Created)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        return group;
    }

    private static async Task<IResult> Handle(CreateConversationRequest? request, IGrainFactory grains)
    {
        var conversationId = Guid.NewGuid();
        var grain = grains.GetGrain<IConversationGrain>(conversationId);
        var response = await grain.InitializeAsync(request?.Title ?? string.Empty, request?.Model ?? string.Empty);
        return Results.Created($"/api/conversations/{response.ConversationId}/history", response);
    }
}
