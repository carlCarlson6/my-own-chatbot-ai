namespace MyOwnChatbotAi.Api.Authentication;

public sealed class ClerkAuthenticationOptions
{
    public const string SectionName = "Clerk";

    public string? Authority { get; init; }

    public string? Audience { get; init; }

    public string? JwksUrl { get; init; }

    public string? JwtVerificationPublicKey { get; init; }

    public bool RequireHttpsMetadata { get; init; } = true;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ResolvedJwksUrl) ||
        !string.IsNullOrWhiteSpace(JwtVerificationPublicKey);

    public string? ResolvedJwksUrl =>
        !string.IsNullOrWhiteSpace(JwksUrl)
            ? JwksUrl
            : !string.IsNullOrWhiteSpace(Authority)
                ? $"{Authority.TrimEnd('/')}/.well-known/jwks.json"
                : null;
}
