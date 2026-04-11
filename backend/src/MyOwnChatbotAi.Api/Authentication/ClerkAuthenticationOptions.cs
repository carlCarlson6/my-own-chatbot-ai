namespace MyOwnChatbotAi.Api.Authentication;

public sealed class ClerkAuthenticationOptions
{
    public const string SectionName = "Clerk";

    public string? Authority { get; init; }

    public string? Audience { get; init; }

    public bool RequireHttpsMetadata { get; init; } = true;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Authority);
}
