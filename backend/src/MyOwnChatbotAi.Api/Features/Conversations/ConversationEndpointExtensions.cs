namespace MyOwnChatbotAi.Api.Features.Conversations;

public static class ConversationEndpointExtensions
{
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/conversations").WithTags("Conversations");

        group.MapCreateConversationEndpoint();
        group.MapSendMessageEndpoint();
        group.MapGetConversationHistoryEndpoint();

        return app;
    }
}
