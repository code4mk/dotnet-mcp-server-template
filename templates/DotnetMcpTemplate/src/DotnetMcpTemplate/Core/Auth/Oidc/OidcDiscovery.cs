using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>Reads and caches the IdP's discovery document and signing keys (keys refresh automatically).</summary>
public sealed class OidcDiscovery
{
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _manager;

    public OidcDiscovery(OidcSettings settings, IHttpClientFactory httpClientFactory)
    {
        var retriever = new HttpDocumentRetriever(httpClientFactory.CreateClient(OidcAuthProvider.HttpClientName))
        {
            RequireHttps = !IsLoopback(settings.DiscoveryUrl),
        };
        _manager = new ConfigurationManager<OpenIdConnectConfiguration>(
            settings.DiscoveryUrl, new OpenIdConnectConfigurationRetriever(), retriever);
    }

    public Task<OpenIdConnectConfiguration> GetAsync(CancellationToken cancellationToken) =>
        _manager.GetConfigurationAsync(cancellationToken);

    /// <summary>Forces a reload (e.g. the IdP rotated its signing keys).</summary>
    public void RequestRefresh() => _manager.RequestRefresh();

    private static bool IsLoopback(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.IsLoopback;
}
