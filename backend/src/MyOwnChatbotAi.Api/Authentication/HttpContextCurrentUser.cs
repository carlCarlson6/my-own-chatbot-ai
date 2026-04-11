using System.Security.Claims;

namespace MyOwnChatbotAi.Api.Authentication;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public ClaimsPrincipal Principal =>
        httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());

    public IReadOnlyCollection<Claim> Claims => Principal.Claims.ToArray();

    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;

    public string? UserId => CurrentUserClaimTypes.ResolveUserId(Principal);
}
