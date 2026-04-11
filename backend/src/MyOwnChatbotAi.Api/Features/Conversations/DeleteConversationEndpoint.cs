using MyOwnChatbotAi.Api.Authentication;
using MyOwnChatbotAi.Api.Contracts;
using MyOwnChatbotAi.Api.Grains;

namespace MyOwnChatbotAi.Api.Features.Conversations;

public static class DeleteConversationEndpoint
{
    private const string Route = "/{conversationId:guid}";

    public static RouteGroupBuilder MapDeleteConversationEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete(Route, Handle)
            .WithName("DeleteConversation")
            .RequireConversationManagementAccess()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiError>(StatusCodes.Status404NotFound)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        return group;
    }

    private static async Task<IResult> Handle(
        Guid conversationId,
        IGrainFactory grains,
        ICurrentUser currentUser)
    {
        var grain = grains.GetGrain<IConversationGrain>(conversationId);
        var result = await grain.DeleteAsync(currentUser.UserId!);

        return result.Outcome switch
        {
            ConversationAccessOutcome.Success => Results.NoContent(),
            ConversationAccessOutcome.Forbidden => Results.Json(
                new ApiError("forbidden", "You do not have access to this conversation."),
                statusCode: StatusCodes.Status403Forbidden),
            _ => Results.NotFound(
                new ApiError("conversation_not_found", $"Conversation '{conversationId}' was not found.", "conversationId"))
        };
    }
}
