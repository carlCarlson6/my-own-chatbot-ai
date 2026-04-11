using System.Security.Claims;

namespace MyOwnChatbotAi.Api.Authentication;

public static class CurrentUserClaimTypes
{
    public const string UserId = "sub";

    private static readonly string[] UserIdClaimTypes =
    [
        UserId,
        ClaimTypes.NameIdentifier
    ];

    public static string? ResolveUserId(ClaimsPrincipal principal)
    {
        foreach (var claimType in UserIdClaimTypes)
        {
            var value = principal.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
