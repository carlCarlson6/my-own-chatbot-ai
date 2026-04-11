using MyOwnChatbotAi.Api.Authentication;
using MyOwnChatbotAi.Api.Contracts;

namespace MyOwnChatbotAi.Api.Features.Conversations;

public static class ConversationManagementAuthorizationExtensions
{
    public static RouteGroupBuilder RequireConversationManagementAccess(this RouteGroupBuilder group)
    {
        group.RequireAuthorization(ConversationAuthorizationPolicies.ConversationManagement);
        return group;
    }

    public static RouteHandlerBuilder RequireConversationManagementAccess(this RouteHandlerBuilder builder)
    {
        builder.RequireAuthorization(ConversationAuthorizationPolicies.ConversationManagement);
        builder.Produces<ApiError>(StatusCodes.Status401Unauthorized);
        builder.Produces<ApiError>(StatusCodes.Status403Forbidden);
        return builder;
    }
}
