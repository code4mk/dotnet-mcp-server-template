using System.Security.Claims;
using System.Text.Encodings.Web;
using DotnetMcpTemplate.Core.Auth;
using DotnetMcpTemplate.Core.Auth.Providers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotnetMcpTemplate.IntegrationTests.TestInfrastructure;

/// <summary>
/// AUTH_PROVIDER=test. A working example of a custom provider: found automatically because this assembly
/// references the app. Token format: <c>Bearer test|{userId}|{space-separated scopes}</c>.
/// </summary>
public sealed class TestAuthProvider : IAuthProvider
{
    public const string SchemeName = "Test";

    public string Name => "test";

    public void Configure(AuthProviderContext context)
    {
        context.Authentication.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(SchemeName, _ => { });
        context.BearerScheme = SchemeName;
        context.AuthorizationServers.Add(context.PublicUrl);
    }

    public static string Token(string userId, string scopes = "mcp:tools") => $"test|{userId}|{scopes}";
}

public sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer test|", StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var parts = header["Bearer ".Length..].Split('|');
        var claims = new List<Claim>
        {
            new(AppClaims.Subject, parts[1]),
            new(AppClaims.Name, "Test " + parts[1]),
            new(AppClaims.Scope, parts.Length > 2 ? parts[2] : string.Empty),
        };

        var identity = new ClaimsIdentity(claims, TestAuthProvider.SchemeName, AppClaims.Name, ClaimTypes.Role);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), TestAuthProvider.SchemeName)));
    }
}
