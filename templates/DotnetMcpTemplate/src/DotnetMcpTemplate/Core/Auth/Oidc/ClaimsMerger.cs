using System.Text.Json.Nodes;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>
/// Builds one identity from the ID token and the userinfo response: protocol claims are dropped, userinfo values
/// win for the same claim, and userinfo must describe the same subject (OIDC Core 5.3.2).
/// </summary>
public sealed class ClaimsMerger
{
    private static readonly HashSet<string> ProtocolClaims = new(StringComparer.Ordinal)
    {
        "iss", "aud", "exp", "iat", "nbf", "nonce", "at_hash", "c_hash", "azp", "jti", "sid", "typ", "auth_time", "acr", "amr",
    };

    public JsonObject Merge(JsonObject idTokenClaims, JsonObject? userInfo)
    {
        var subject = idTokenClaims["sub"]?.GetValue<string>()
            ?? throw new OAuthException("server_error", "The ID token has no sub claim.");

        var merged = new JsonObject();
        foreach (var (name, value) in idTokenClaims)
        {
            if (!ProtocolClaims.Contains(name))
            {
                merged[name] = value?.DeepClone();
            }
        }

        if (userInfo is not null)
        {
            var userInfoSubject = userInfo["sub"]?.ToString();
            if (!string.Equals(userInfoSubject, subject, StringComparison.Ordinal))
            {
                throw new OAuthException("server_error", "The userinfo response is for a different user than the ID token.");
            }

            foreach (var (name, value) in userInfo)
            {
                merged[name] = value?.DeepClone();
            }
        }

        return merged;
    }
}
