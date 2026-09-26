using DotnetMcpTemplate.Core.Auth.Oidc;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetMcpTemplate.IntegrationTests.TestInfrastructure;

/// <summary>The server with the default oidc provider, talking to <see cref="FakeIdentityProvider"/>.</summary>
public class OidcProxyServerFactory : McpServerFactory
{
    public OidcProxyServerFactory() : this(new Dictionary<string, string?>())
    {
    }

    /// <summary>Overrides on top of the defaults below, e.g. <c>AUTH_STORE=file</c>.</summary>
    protected OidcProxyServerFactory(IDictionary<string, string?> overrides) : base(Merge(overrides))
    {
    }

    public static FakeIdentityProvider Idp { get; } = new();

    public TestClock Clock { get; } = new();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton<TimeProvider>(Clock);
        services.AddHttpClient(OidcAuthProvider.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => Idp);
    }

    private static Dictionary<string, string?> Merge(IDictionary<string, string?> overrides)
    {
        var settings = new Dictionary<string, string?>
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
            ["AUTH_STORE_PATH"] = null,
            ["AUTH_REFRESH_REUSE_SECONDS"] = "30",
        };
        foreach (var (key, value) in overrides)
        {
            settings[key] = value;
        }

        return settings;
    }
}

/// <summary>The oidc server with AUTH_STORE=file in the given folder: several instances in a row share sign-ins.</summary>
public sealed class FileStoreOidcServerFactory(string storePath)
    : OidcProxyServerFactory(new Dictionary<string, string?> { ["AUTH_STORE"] = "file", ["AUTH_STORE_PATH"] = storePath });
