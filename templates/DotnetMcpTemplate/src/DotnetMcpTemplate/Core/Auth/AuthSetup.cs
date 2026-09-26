using DotnetMcpTemplate.Core.Auth.Dev;
using DotnetMcpTemplate.Core.Auth.Providers;
using DotnetMcpTemplate.Core.Common.Settings;
using DotnetMcpTemplate.Core.Mcp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;

namespace DotnetMcpTemplate.Core.Auth;

public static class AuthSetup
{
    /// <summary>
    /// Selects the auth provider (AUTH_PROVIDER), adds the MCP challenge and protected resource metadata,
    /// the policies and the injectable <see cref="AppUser"/>.
    /// </summary>
    public static IServiceCollection AddAppAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var app = configuration.ReadEnvSettings<AppSettings>();
        var auth = configuration.ReadEnvSettings<AuthSettings>();
        var mcp = configuration.ReadEnvSettings<McpSettings>();
        var resourceUrl = app.PublicUrl + mcp.NormalizedPath;

        services.AddHttpContextAccessor();
        services.AddScoped(sp => AppUser.FromPrincipal(sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User));
        services.AddAppPolicies();
        services.AddCors(options => options.AddPolicy(OAuthCors.PolicyName, policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("WWW-Authenticate")));

        if (auth.Mode == AuthModes.None)
        {
            if (!app.IsDev)
            {
                throw new InvalidOperationException("MCP_AUTH_MODE=none is only allowed when APP_ENV=dev.");
            }

            services.AddAuthentication(DevAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(DevAuthenticationHandler.SchemeName, _ => { });
            services.TryAddSingleton<IUpstreamTokenAccessor, NoUpstreamTokenAccessor>();
            return services;
        }

        var provider = AuthProviderRegistry.Get(auth.Provider);
        var authentication = services.AddAuthentication();
        var context = new AuthProviderContext(services, configuration, authentication, app.PublicUrl, resourceUrl);
        provider.Configure(context);

        var bearerScheme = context.BearerScheme
            ?? throw new InvalidOperationException($"Auth provider '{provider.Name}' did not set BearerScheme.");

        services.Configure<AuthenticationOptions>(options =>
        {
            options.DefaultScheme = bearerScheme;
            options.DefaultAuthenticateScheme = bearerScheme;
            // The MCP scheme answers 401s with WWW-Authenticate: Bearer resource_metadata="...",
            // which tells MCP clients where to log in.
            options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
        });

        authentication.AddMcp(options => options.ResourceMetadata = new ProtectedResourceMetadata
        {
            Resource = resourceUrl,
            AuthorizationServers = context.AuthorizationServers,
            ScopesSupported = AppScopes.All.ToList(),
            ResourceName = mcp.ServerName,
        });

        services.TryAddSingleton<IUpstreamTokenAccessor, NoUpstreamTokenAccessor>();
        services.AddSingleton(new ActiveAuthProvider(provider));
        return services;
    }

    /// <summary>Maps the active provider's endpoints (for oidc: the OAuth proxy).</summary>
    public static WebApplication MapAppAuth(this WebApplication app)
    {
        app.Services.GetService<ActiveAuthProvider>()?.Provider.MapEndpoints(app);
        return app;
    }
}

public sealed record ActiveAuthProvider(IAuthProvider Provider);

public static class OAuthCors
{
    /// <summary>Public OAuth endpoints (metadata, register, token) are called from browser-based MCP clients.</summary>
    public const string PolicyName = "oauth-public";
}
