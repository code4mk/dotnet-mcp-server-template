using DotnetMcpTemplate.Core.Auth.Providers;
using DotnetMcpTemplate.Core.Common.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DotnetMcpTemplate.Core.Auth.Jwt;

/// <summary>
/// AUTH_PROVIDER=jwt: the MCP server is a plain resource server and validates access tokens issued by the IdP
/// (signature via JWKS, issuer, audience, lifetime). Use it when your IdP already supports MCP clients directly
/// (dynamic client registration or pre-registered clients) and issues JWT access tokens with your MCP URL as audience.
/// Also a compact example of a custom provider.
/// </summary>
public sealed class JwtAuthProvider : IAuthProvider
{
    public const string SchemeName = "IdpJwt";

    public string Name => "jwt";

    public void Configure(AuthProviderContext context)
    {
        context.Services.AddEnvSettings<JwtProviderSettings>(context.Configuration);
        var settings = context.Configuration.ReadEnvSettings<JwtProviderSettings>();

        context.Authentication.AddJwtBearer(SchemeName, _ => { });
        context.Services.AddOptions<JwtBearerOptions>(SchemeName)
            .Configure<JwtProviderSettings>((options, jwt) =>
            {
                options.MetadataAddress = jwt.DiscoveryUrl;
                options.RequireHttpsMetadata = !IsLocal(jwt.DiscoveryUrl);
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudiences = jwt.Audiences(context.ResourceUrl),
                    ValidateLifetime = true,
                    NameClaimType = AppClaims.Name,
                };
            });

        context.BearerScheme = SchemeName;
        context.AuthorizationServers.Add(settings.Issuer);
    }

    private static bool IsLocal(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.IsLoopback;
}
