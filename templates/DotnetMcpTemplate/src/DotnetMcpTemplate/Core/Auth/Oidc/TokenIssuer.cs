using System.Text.Json;
using System.Text.Json.Nodes;
using DotnetMcpTemplate.Core.Common.Settings;
using DotnetMcpTemplate.Core.Mcp;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>
/// Issues the server's access tokens: a JWT (HS256) with the merged identity's core claims, the granted scopes, any
/// OIDC_TOKEN_CLAIMS, and whatever the registered <see cref="ITokenClaimsEnricher"/>s add. Audience = the MCP URL,
/// issuer = APP_URL.
/// </summary>
public sealed class TokenIssuer(
    AuthCrypto crypto,
    OidcSettings oidc,
    OidcProxySettings proxy,
    AppSettings app,
    McpSettings mcp,
    TimeProvider time,
    IServiceScopeFactory scopeFactory)
{
    private static readonly string[] StandardClaims =
    [
        AppClaims.Name, AppClaims.Email, AppClaims.EmailVerified, AppClaims.PreferredUsername, AppClaims.Picture,
        "given_name", "family_name", "locale",
    ];

    private readonly JsonWebTokenHandler _handler = new();

    public TimeSpan Lifetime => TimeSpan.FromMinutes(proxy.TokenLifetimeMinutes);

    public async Task<string> CreateAccessTokenAsync(UserSession session, IReadOnlyList<string> scopes, TokenIssue reason, CancellationToken cancellationToken)
    {
        var merged = JsonNode.Parse(session.MergedClaimsJson)!.AsObject();
        var now = time.GetUtcNow().UtcDateTime;

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [AppClaims.Subject] = session.Subject,
            [AppClaims.SessionId] = session.Id,
            [AppClaims.ClientId] = session.ClientId,
            [AppClaims.IdentityProvider] = session.Issuer,
            [AppClaims.Scope] = string.Join(' ', scopes),
            ["jti"] = AuthCrypto.RandomToken(16),
        };

        foreach (var name in StandardClaims.Concat(oidc.TokenClaims.SplitList()))
        {
            if (merged.TryGetPropertyValue(name, out var value) && value is not null)
            {
                claims[name] = JsonSerializer.SerializeToElement(value);
            }
        }

        await EnrichAsync(session, scopes, reason, merged, claims, cancellationToken);

        return _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = app.PublicUrl,
            Audience = app.PublicUrl + mcp.NormalizedPath,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.Add(Lifetime),
            Claims = claims,
            SigningCredentials = new SigningCredentials(crypto.AccessTokenKey, SecurityAlgorithms.HmacSha256),
        });
    }

    /// <summary>Runs the registered enrichers in a DI scope, then restores the protected claims.</summary>
    private async Task EnrichAsync(
        UserSession session, IReadOnlyList<string> scopes, TokenIssue reason, JsonObject merged, Dictionary<string, object> claims,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var enrichers = scope.ServiceProvider.GetServices<ITokenClaimsEnricher>().ToArray();
        if (enrichers.Length == 0)
        {
            return;
        }

        var protectedValues = claims.Where(c => TokenClaimsContext.Protected.Contains(c.Key)).ToArray();
        var context = new TokenClaimsContext(
            reason,
            session.Subject,
            session.ClientId,
            scopes,
            IdpClaims.Parse(session.IdTokenJson),
            IdpClaims.Parse(session.UserInfoJson),
            (JsonObject)merged.DeepClone(),
            IdpClaims.Parse(session.SessionDataJson) ?? new JsonObject(),
            claims);
        foreach (var enricher in enrichers)
        {
            await enricher.EnrichAsync(context, cancellationToken);
        }

        foreach (var name in TokenClaimsContext.Protected)
        {
            claims.Remove(name);
        }

        foreach (var (name, value) in protectedValues)
        {
            claims[name] = value;
        }
    }
}
