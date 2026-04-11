using System.Security.Claims;

namespace MyOwnChatbotAi.Api.Authentication;

public interface ICurrentUser
{
    ClaimsPrincipal Principal { get; }

    IReadOnlyCollection<Claim> Claims { get; }

    bool IsAuthenticated { get; }

    string? UserId { get; }
}
