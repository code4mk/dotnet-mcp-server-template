using System.Text.Json;
using System.Text.Json.Nodes;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>
/// Builds one identity from the ID token and the userinfo response: protocol claims are dropped, userinfo values
/// win for the same claim, and userinfo must describe the same subject (OIDC Core 5.3.2).
/// </summary>
public sealed class ClaimsMerger(OidcSettings settings)
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

    public IReadOnlyList<string> Roles(JsonObject claims) => ReadList(claims, settings.RoleClaim);

    public IReadOnlyList<string> Groups(JsonObject claims) => ReadList(claims, settings.GroupsClaim);

    /// <summary>Reads a string, array or space-separated value at a (dot) path, e.g. realm_access.roles.</summary>
    public static IReadOnlyList<string> ReadList(JsonObject claims, string path)
    {
        JsonNode? node = claims;
        foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            node = node is JsonObject obj && obj.TryGetPropertyValue(part, out var child) ? child : null;
        }

        return node switch
        {
            JsonArray array => array.Select(item => item?.ToString()).Where(v => !string.IsNullOrEmpty(v)).Cast<string>().Distinct().ToArray(),
            JsonValue value when value.GetValueKind() == JsonValueKind.String =>
                value.GetValue<string>().Split(' ', StringSplitOptions.RemoveEmptyEntries),
            _ => [],
        };
    }
}
