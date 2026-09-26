using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DotnetMcpTemplate.Core.Auth.Dev;

/// <summary>
/// MCP_AUTH_MODE=none (dev only): every request is signed in as a fixed developer user with every scope and the
/// admin role, so all tools can be tried without an identity provider.
/// </summary>
internal sealed class DevAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Dev";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new(AppClaims.Subject, "dev-user"),
            new(AppClaims.Name, "Developer"),
            new(AppClaims.Email, "dev@localhost"),
            new(AppClaims.PreferredUsername, "dev"),
            new(AppClaims.Scope, string.Join(' ', AppScopes.All)),
            new(AppClaims.IdentityProvider, "dev"),
        };

        var identity = new ClaimsIdentity(claims, SchemeName, AppClaims.Name, ClaimTypes.Role);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
