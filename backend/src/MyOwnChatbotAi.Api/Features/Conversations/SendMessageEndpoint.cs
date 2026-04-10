using MyOwnChatbotAi.Api.Contracts;
using MyOwnChatbotAi.Api.Grains;

namespace MyOwnChatbotAi.Api.Features.Conversations;

public static class SendMessageEndpoint
{
    private const int MaxMessageLength = 8_000;

    public static RouteGroupBuilder MapSendMessageEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost(
            "/send",
            async (SendMessageRequest request, IGrainFactory grains) =>
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
                    var response = await grain.SendMessageAsync(request.Message, request.Model);
                    return Results.Ok(response);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status502BadGateway,
                        title: "Upstream model error");
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
