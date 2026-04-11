using MyOwnChatbotAi.Api.Contracts;
using MyOwnChatbotAi.Api.Authentication;
using MyOwnChatbotAi.Api.Grains;

namespace MyOwnChatbotAi.Api.Features.Conversations;

public static class SendMessageEndpoint
{
    private const string Route = "/send";
    private const int MaxMessageLength = 8_000;

    public static RouteGroupBuilder MapSendMessageEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost(Route, Handle)
            .WithName("SendMessage")
            .Produces<SendMessageResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces<ApiError>(StatusCodes.Status404NotFound)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        return group;
    }

    private static async Task<IResult> Handle(
        SendMessageRequest request,
        IGrainFactory grains,
        ICurrentUser currentUser)
    {
        if (request.Message is null || string.IsNullOrWhiteSpace(request.Message.Content))
        {
            return Results.BadRequest(new ApiError("validation_error", "Message content is required.", "message"));
        }

        if (request.Message.Content.Length > MaxMessageLength)
        {
            return Results.BadRequest(new ApiError(
                "validation_error",
                $"Message content must not exceed {MaxMessageLength} characters.",
                "message"));
        }

        var conversationId = request.ConversationId ?? Guid.NewGuid();
        var grain = grains.GetGrain<IConversationGrain>(conversationId);

        try
        {
            var response = await grain.SendMessageAsync(
                currentUser.UserId,
                request.Message,
                request.ConversationId is null);

            return response is null
                ? Results.NotFound(new ApiError("conversation_not_found", $"Conversation '{conversationId}' was not found.", "conversationId"))
                : Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(
                new ApiError("upstream_model_error", ex.Message),
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
