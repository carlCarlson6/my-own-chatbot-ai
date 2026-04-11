using MyOwnChatbotAi.Api.Authentication;
using MyOwnChatbotAi.Api.Contracts;
using MyOwnChatbotAi.Api.Grains;

namespace MyOwnChatbotAi.Api.Features.Conversations;

public static class RenameConversationEndpoint
{
    private const string Route = "/{conversationId:guid}";
    private const int MaxTitleLength = 120;

    public static RouteGroupBuilder MapRenameConversationEndpoint(this RouteGroupBuilder group)
    {
        group.MapPatch(Route, Handle)
            .WithName("RenameConversation")
            .RequireConversationManagementAccess()
            .Produces<ConversationSummary>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces<ApiError>(StatusCodes.Status404NotFound)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        return group;
    }

    private static async Task<IResult> Handle(
        Guid conversationId,
        RenameConversationRequest request,
        IGrainFactory grains,
        ICurrentUser currentUser)
    {
        var normalizedTitle = request.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            return Results.BadRequest(new ApiError("validation_error", "Title is required.", "title"));
        }

        if (normalizedTitle.Length > MaxTitleLength)
        {
            return Results.BadRequest(new ApiError(
                "validation_error",
                $"Title must not exceed {MaxTitleLength} characters.",
                "title"));
        }

        var grain = grains.GetGrain<IConversationGrain>(conversationId);
        var result = await grain.RenameAsync(currentUser.UserId!, normalizedTitle);

        return result.Outcome switch
        {
            ConversationAccessOutcome.Success => Results.Ok(result.Conversation),
            ConversationAccessOutcome.Forbidden => Results.Json(
                new ApiError("forbidden", "You do not have access to this conversation."),
                statusCode: StatusCodes.Status403Forbidden),
            _ => Results.NotFound(
                new ApiError("conversation_not_found", $"Conversation '{conversationId}' was not found.", "conversationId"))
        };
    }
}
