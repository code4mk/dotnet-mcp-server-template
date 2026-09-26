using System.Text.Json;
using System.Text.Json.Nodes;
using DotnetMcpTemplate.Core.Common.Settings;
using DotnetMcpTemplate.Core.Mcp;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>
/// Issues the server's access tokens: a JWT (HS256) with the merged identity's core claims, roles, groups,
/// granted scopes and any OIDC_TOKEN_CLAIMS. Audience = the MCP URL, issuer = APP_URL.
/// </summary>
public sealed class TokenIssuer(
    AuthCrypto crypto,
    OidcSettings oidc,
    OidcProxySettings proxy,
    AppSettings app,
    McpSettings mcp,
    ClaimsMerger merger,
    TimeProvider time)
{
    private static readonly string[] StandardClaims =
    [
        AppClaims.Name, AppClaims.Email, AppClaims.EmailVerified, AppClaims.PreferredUsername, AppClaims.Picture,
        "given_name", "family_name", "locale",
    ];

    private readonly JsonWebTokenHandler _handler = new();

    public TimeSpan Lifetime => TimeSpan.FromMinutes(proxy.TokenLifetimeMinutes);

    public string CreateAccessToken(UserSession session, IReadOnlyList<string> scopes)
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

        if (merger.Roles(merged) is { Count: > 0 } roles)
        {
            claims[AppClaims.Roles] = roles.ToArray();
        }

        if (merger.Groups(merged) is { Count: > 0 } groups)
        {
            claims[AppClaims.Groups] = groups.ToArray();
        }

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
}
