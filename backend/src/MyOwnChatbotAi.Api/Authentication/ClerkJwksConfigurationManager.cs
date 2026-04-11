using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace MyOwnChatbotAi.Api.Authentication;

public sealed class ClerkJwksConfigurationManager(
    string jwksUrl,
    bool requireHttpsMetadata) : IConfigurationManager<OpenIdConnectConfiguration>, IDisposable
{
    private static readonly TimeSpan AutomaticRefreshInterval = TimeSpan.FromHours(12);
    private static readonly TimeSpan MinimumRefreshInterval = TimeSpan.FromMinutes(5);

    private readonly HttpClient httpClient = CreateHttpClient(jwksUrl, requireHttpsMetadata);
    private readonly SemaphoreSlim refreshLock = new(1, 1);
    private OpenIdConnectConfiguration? currentConfiguration;
    private DateTimeOffset lastRefreshUtc = DateTimeOffset.MinValue;
    private DateTimeOffset lastRefreshRequestUtc = DateTimeOffset.MinValue;
    private bool refreshRequested;
    private bool disposed;

    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (TryGetCachedConfiguration(out var cachedConfiguration))
        {
            return cachedConfiguration;
        }

        await refreshLock.WaitAsync(cancel);

        try
        {
            if (TryGetCachedConfiguration(out cachedConfiguration))
            {
                return cachedConfiguration;
            }

            currentConfiguration = await LoadConfigurationAsync(cancel);
            lastRefreshUtc = DateTimeOffset.UtcNow;
            refreshRequested = false;

            return currentConfiguration;
        }
        finally
        {
            refreshLock.Release();
        }
    }

    public void RequestRefresh()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var now = DateTimeOffset.UtcNow;

        if (now - lastRefreshRequestUtc < MinimumRefreshInterval)
        {
            return;
        }

        refreshRequested = true;
        lastRefreshRequestUtc = now;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        refreshLock.Dispose();
        httpClient.Dispose();
    }

    private async Task<OpenIdConnectConfiguration> LoadConfigurationAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        using var response = await httpClient.GetAsync(jwksUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var document = await response.Content.ReadAsStringAsync(cancellationToken);
        var keySet = new JsonWebKeySet(document);
        var configuration = new OpenIdConnectConfiguration();

        foreach (var key in keySet.Keys)
        {
            configuration.SigningKeys.Add(key);
        }

        return configuration;
    }

    private bool TryGetCachedConfiguration(out OpenIdConnectConfiguration configuration)
    {
        if (currentConfiguration is not null &&
            !refreshRequested &&
            DateTimeOffset.UtcNow - lastRefreshUtc < AutomaticRefreshInterval)
        {
            configuration = currentConfiguration;
            return true;
        }

        configuration = null!;
        return false;
    }

    private static HttpClient CreateHttpClient(string url, bool requireHttpsMetadata)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("Clerk JWKS URL must be an absolute URI.");
        }

        if (requireHttpsMetadata && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Clerk JWKS URL must use HTTPS when RequireHttpsMetadata is enabled.");
        }

        return new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
    }
}
