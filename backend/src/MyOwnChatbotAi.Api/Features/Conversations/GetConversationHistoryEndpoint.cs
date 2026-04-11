using MyOwnChatbotAi.Api.Contracts;
using MyOwnChatbotAi.Api.Authentication;
using MyOwnChatbotAi.Api.Grains;

namespace MyOwnChatbotAi.Api.Features.Conversations;

public static class GetConversationHistoryEndpoint
{
    private const string Route = "/{conversationId:guid}/history";

    public static RouteGroupBuilder MapGetConversationHistoryEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet(Route, Handle)
            .WithName("GetConversationHistory")
            .Produces<GetConversationHistoryResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        return group;
    }

    private static async Task<IResult> Handle(Guid conversationId, IGrainFactory grains, ICurrentUser currentUser)
    {
        var grain = grains.GetGrain<IConversationGrain>(conversationId);
        var result = await grain.GetHistoryAsync(currentUser.UserId);

        return result.Outcome switch
        {
            ConversationAccessOutcome.Success => Results.Ok(result.Response),
            ConversationAccessOutcome.AuthenticationRequired => Results.Json(
                new ApiError("authentication_required", "Sign-in is required for this operation."),
                statusCode: StatusCodes.Status401Unauthorized),
            ConversationAccessOutcome.Forbidden => Results.Json(
                new ApiError("forbidden", "You do not have access to this conversation."),
                statusCode: StatusCodes.Status403Forbidden),
            _ => Results.NotFound(
                new ApiError("conversation_not_found", $"Conversation '{conversationId}' was not found.", "conversationId"))
        };
    }
}
