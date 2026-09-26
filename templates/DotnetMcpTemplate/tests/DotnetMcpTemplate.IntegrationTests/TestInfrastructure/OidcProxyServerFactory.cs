using DotnetMcpTemplate.Core.Auth.Oidc;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetMcpTemplate.IntegrationTests.TestInfrastructure;

/// <summary>The server with the default oidc provider, talking to <see cref="FakeIdentityProvider"/>.</summary>
public sealed class OidcProxyServerFactory : McpServerFactory
{
    public OidcProxyServerFactory() : base(new Dictionary<string, string?>
    {
        ["AUTH_PROVIDER"] = "oidc",
        ["OIDC_DISCOVERY_URL"] = FakeIdentityProvider.Issuer + "/.well-known/openid-configuration",
        ["OIDC_CLIENT_ID"] = FakeIdentityProvider.ClientId,
        ["OIDC_CLIENT_SECRET"] = FakeIdentityProvider.ClientSecret,
        ["OIDC_CLIENT_AUTH_METHOD"] = "client_secret_basic",
        ["OIDC_SCOPES"] = "openid,profile,email",
        ["OIDC_USERINFO"] = "auto",
        ["OIDC_ROLE_CLAIM"] = "roles",
        ["OIDC_TOKEN_CLAIMS"] = "department",
        ["AUTH_TOKEN_SIGNING_KEY"] = "integration-tests-signing-key-0123456789abcdef",
        ["AUTH_ALLOWED_REDIRECT_URIS"] = "http://localhost:*",
        ["AUTH_STORE"] = "memory",
    })
    {
    }

    public static FakeIdentityProvider Idp { get; } = new();

    protected override void ConfigureTestServices(IServiceCollection services) =>
        services.AddHttpClient(OidcAuthProvider.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => Idp);
}
