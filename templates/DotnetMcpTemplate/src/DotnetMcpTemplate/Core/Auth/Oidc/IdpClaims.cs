using System.Text.Json;
using System.Text.Json.Nodes;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>Reads identity provider claims, whatever their shape.</summary>
public static class IdpClaims
{
    /// <summary>
    /// Values at a dot path: a string, an array, or a space-separated string, e.g. <c>groups</c>,
    /// <c>realm_access.roles</c> (Keycloak), <c>cognito:groups</c> (Cognito). Empty when missing.
    /// </summary>
    public static IReadOnlyList<string> Values(JsonObject? claims, string path)
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
            JsonValue value => [value.ToString()],
            _ => [],
        };
    }

    internal static JsonObject? Parse(string? json) => string.IsNullOrEmpty(json) ? null : JsonNode.Parse(json)?.AsObject();
}
