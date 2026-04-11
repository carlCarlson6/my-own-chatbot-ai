using MyOwnChatbotAi.Api.Authentication;
using MyOwnChatbotAi.Api.Contracts;
using MyOwnChatbotAi.Api.Features.Conversations.Persistence;

namespace MyOwnChatbotAi.Api.Features.Conversations;

public static class ListConversationsEndpoint
{
    private const string Route = "";

    public static RouteGroupBuilder MapListConversationsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet(Route, Handle)
            .WithName("ListConversations")
            .RequireConversationManagementAccess()
            .Produces<IReadOnlyList<ConversationSummary>>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status500InternalServerError);

        return group;
    }

    private static async Task<IResult> Handle(
        IUserOwnedConversationStore conversationStore,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var conversations = await conversationStore.ListAsync(currentUser.UserId!, cancellationToken);
        return Results.Ok(conversations.Select(static conversation => conversation.ToContract()).ToArray());
    }
}
