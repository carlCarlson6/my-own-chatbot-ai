namespace MyOwnChatbotAi.Api.Authentication;

public sealed class ClerkAuthenticationOptions
{
    public const string SectionName = "Clerk";

    public string? JwtVerificationPublicKey { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(JwtVerificationPublicKey);
}
