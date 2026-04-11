namespace MyOwnChatbotAi.Api.Authentication;

public sealed class ClerkAuthenticationOptions
{
    public const string SectionName = "Clerk";

    public string? JwksUrl { get; init; }

    public string? JwtVerificationPublicKey { get; init; }

    public bool RequireHttpsMetadata { get; init; } = true;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ResolvedJwksUrl) ||
        !string.IsNullOrWhiteSpace(JwtVerificationPublicKey);

    public string? ResolvedJwksUrl =>
        !string.IsNullOrWhiteSpace(JwksUrl)
            ? JwksUrl.Trim()
            : null;
}
