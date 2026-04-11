using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace MyOwnChatbotAi.Api.Authentication;

public static class ClerkAuthenticationExtensions
{
    public static IServiceCollection AddClerkAuthenticationFoundation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.Configure<ClerkAuthenticationOptions>(
            configuration.GetSection(ClerkAuthenticationOptions.SectionName));

        var clerkOptions = configuration
            .GetSection(ClerkAuthenticationOptions.SectionName)
            .Get<ClerkAuthenticationOptions>() ?? new ClerkAuthenticationOptions();

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = ConversationAuthenticationDefaults.Scheme;
                options.DefaultAuthenticateScheme = ConversationAuthenticationDefaults.Scheme;
                options.DefaultChallengeScheme = ConversationAuthenticationDefaults.Scheme;
                options.DefaultForbidScheme = ConversationAuthenticationDefaults.Scheme;
            })
            .AddPolicyScheme(ConversationAuthenticationDefaults.Scheme, "Optional Clerk bearer authentication", options =>
            {
                options.ForwardDefaultSelector = _ => clerkOptions.IsConfigured
                    ? JwtBearerDefaults.AuthenticationScheme
                    : ConversationAuthenticationDefaults.DisabledScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = clerkOptions.RequireHttpsMetadata;
                options.SaveToken = false;

                if (!string.IsNullOrWhiteSpace(clerkOptions.Authority))
                {
                    options.Authority = clerkOptions.Authority;
                }

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = CurrentUserClaimTypes.UserId,
                    ValidateAudience = !string.IsNullOrWhiteSpace(clerkOptions.Audience)
                };

                if (!string.IsNullOrWhiteSpace(clerkOptions.Audience))
                {
                    options.Audience = clerkOptions.Audience;
                }
            })
            .AddScheme<AuthenticationSchemeOptions, DisabledAuthenticationHandler>(
                ConversationAuthenticationDefaults.DisabledScheme,
                _ => { });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(ConversationAuthorizationPolicies.ConversationManagement, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(static context =>
                    !string.IsNullOrWhiteSpace(CurrentUserClaimTypes.ResolveUserId(context.User)));
            });
        });

        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthorizationMiddlewareResultHandler>();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        return services;
    }
}

public static class ConversationAuthorizationPolicies
{
    public const string ConversationManagement = "ConversationManagement";
}

public static class ConversationAuthenticationDefaults
{
    public const string Scheme = "ConversationBearer";
    public const string DisabledScheme = "ConversationBearerDisabled";
}
