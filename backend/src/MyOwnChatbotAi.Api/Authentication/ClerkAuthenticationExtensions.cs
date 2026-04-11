using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using MyOwnChatbotAi.Api.Contracts;
using Microsoft.Extensions.Options;

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
                options.ForwardDefaultSelector = context => SelectScheme(context.Request, clerkOptions);
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = clerkOptions.RequireHttpsMetadata;
                options.SaveToken = false;
                options.RefreshOnIssuerKeyNotFound = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = CurrentUserClaimTypes.UserId,
                    ValidateIssuer = !string.IsNullOrWhiteSpace(clerkOptions.Authority),
                    ValidIssuer = clerkOptions.Authority,
                    ValidateAudience = !string.IsNullOrWhiteSpace(clerkOptions.Audience),
                    ValidAudience = clerkOptions.Audience,
                    ValidateIssuerSigningKey = clerkOptions.IsConfigured,
                    RequireSignedTokens = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                if (!string.IsNullOrWhiteSpace(clerkOptions.JwtVerificationPublicKey))
                {
                    options.TokenValidationParameters.IssuerSigningKey =
                        CreateSigningKey(clerkOptions.JwtVerificationPublicKey);
                }
                else if (!string.IsNullOrWhiteSpace(clerkOptions.ResolvedJwksUrl))
                {
                    options.ConfigurationManager = new ClerkJwksConfigurationManager(
                        clerkOptions.ResolvedJwksUrl,
                        clerkOptions.Authority,
                        clerkOptions.RequireHttpsMetadata);
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

    public static IApplicationBuilder UseClerkBearerValidation(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var clerkOptions = context.RequestServices
                .GetRequiredService<IOptions<ClerkAuthenticationOptions>>()
                .Value;

            if (!clerkOptions.IsConfigured || !HasBearerToken(context.Request))
            {
                await next();
                return;
            }

            var authenticationResult = await context.AuthenticateAsync(ConversationAuthenticationDefaults.Scheme);

            if (authenticationResult.Succeeded)
            {
                if (authenticationResult.Principal is not null)
                {
                    context.User = authenticationResult.Principal;
                }

                await next();
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(
                new ApiError("invalid_token", "The Clerk bearer token is expired, invalid, or has been tampered with."));
        });
    }

    private static string SelectScheme(HttpRequest request, ClerkAuthenticationOptions clerkOptions) =>
        clerkOptions.IsConfigured && HasBearerToken(request)
            ? JwtBearerDefaults.AuthenticationScheme
            : ConversationAuthenticationDefaults.DisabledScheme;

    private static bool HasBearerToken(HttpRequest request) =>
        AuthenticationHeaderValue.TryParse(request.Headers.Authorization, out var headerValue) &&
        string.Equals(headerValue.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(headerValue.Parameter);

    private static SecurityKey CreateSigningKey(string rawPublicKey)
    {
        var normalizedPublicKey = rawPublicKey.Trim();

        if (normalizedPublicKey.StartsWith('{'))
        {
            return new JsonWebKey(normalizedPublicKey);
        }

        var rsa = RSA.Create();
        rsa.ImportFromPem(normalizedPublicKey.Replace("\\n", "\n"));
        return new RsaSecurityKey(rsa);
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
