using MyOwnChatbotAi.Api.Contracts;
using MyOwnChatbotAi.Api.Services;

namespace MyOwnChatbotAi.Api.Features.Conversations;

public static class SendMessageEndpoint
{
    public static RouteGroupBuilder MapSendMessageEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost(
            "/send",
            (SendMessageRequest request, IConversationService conversations) =>
            {
                try
                {
                    var response = conversations.SendMessage(request);
                    return Results.Ok(response);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new ApiError("validation_error", ex.Message, "message"));
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(new ApiError("conversation_not_found", ex.Message, "conversationId"));
                }
            })
            .WithName("SendMessage")
            .Produces<SendMessageResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces<ApiError>(StatusCodes.Status404NotFound)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        return group;
    }
}
