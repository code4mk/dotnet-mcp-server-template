using DotnetMcpTemplate.Core.Auth.Providers;
using DotnetMcpTemplate.Core.Common.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>
/// AUTH_PROVIDER=oidc (default): an OAuth proxy that works with every OIDC identity provider, including ones
/// without dynamic client registration. MCP clients register with and log in through this server; the server logs
/// the user in at your IdP with your client id, validates the ID token, merges userinfo and issues its own token.
/// </summary>
public sealed class OidcAuthProvider : IAuthProvider
{
    public const string SchemeName = "McpServerToken";
    public const string HttpClientName = "oidc-upstream";

    public string Name => "oidc";

    public void Configure(AuthProviderContext context)
    {
        var services = context.Services;
        services.AddEnvSettings<OidcSettings>(context.Configuration);
        services.AddEnvSettings<OidcProxySettings>(context.Configuration);

        var proxy = context.Configuration.ReadEnvSettings<OidcProxySettings>();
        if (proxy.Store == "redis")
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = proxy.RedisUrl;
                options.InstanceName = "mcp-auth:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpClient(HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddSingleton<AuthCrypto>();
        services.AddSingleton<AuthStore>();
        services.AddSingleton<OidcDiscovery>();
        services.AddSingleton<UpstreamIdpClient>();
        services.AddSingleton<RedirectUriPolicy>();
        services.AddSingleton<ClaimsMerger>();
        services.AddSingleton<TokenIssuer>();
        services.AddSingleton<OAuthProxy>();
        services.AddSingleton<IUpstreamTokenAccessor, OidcUpstreamTokenAccessor>();

        // Validates the tokens this server issues (HS256, issuer = APP_URL, audience = MCP URL).
        context.Authentication.AddJwtBearer(SchemeName, _ => { });
        services.AddOptions<JwtBearerOptions>(SchemeName)
            .Configure<AuthCrypto>((options, crypto) =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = context.PublicUrl,
                    ValidateAudience = true,
                    ValidAudience = context.ResourceUrl,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = crypto.AccessTokenKey,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = AppClaims.Name,
                    RoleClaimType = AppClaims.Roles,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        context.BearerScheme = SchemeName;
        context.AuthorizationServers.Add(context.PublicUrl);   // clients log in through this server
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => OAuthProxyEndpoints.Map(endpoints);
}
