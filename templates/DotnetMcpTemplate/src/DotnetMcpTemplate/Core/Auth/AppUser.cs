using System.Security.Claims;
using System.Text.Json;

namespace DotnetMcpTemplate.Core.Auth;

/// <summary>
/// The signed-in user, the same shape for every identity provider. Inject it into tools, resources, prompts and
/// services (it is hidden from the tool schema, so the model can't fake it). Anonymous callers get
/// <see cref="IsAuthenticated"/> = false.
/// </summary>
public sealed class AppUser
{
    public static AppUser Anonymous { get; } = new()
    {
        Id = string.Empty,
        IsAuthenticated = false,
    };

    public required string Id { get; init; }

    public bool IsAuthenticated { get; init; }

    public string? Name { get; init; }

    public string? Email { get; init; }

    public string? Username { get; init; }

    public string? Picture { get; init; }

    public IReadOnlyList<string> Scopes { get; init; } = [];

    /// <summary>
    /// Every claim of the token (arrays for repeated claims), including IdP claims copied with OIDC_TOKEN_CLAIMS
    /// such as roles or groups.
    /// </summary>
    public IReadOnlyDictionary<string, JsonElement> Claims { get; init; } = new Dictionary<string, JsonElement>();

    public bool HasScope(string scope) => Scopes.Contains(scope, StringComparer.Ordinal);

    /// <summary>All values of a claim, e.g. <c>user.ClaimValues("roles")</c> after OIDC_TOKEN_CLAIMS=roles.</summary>
    public IReadOnlyList<string> ClaimValues(string name) =>
        !Claims.TryGetValue(name, out var value) ? []
        : value.ValueKind == JsonValueKind.Array ? value.EnumerateArray().Select(v => v.ToString()).ToArray()
        : [value.ToString()];

    public static AppUser FromPrincipal(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return Anonymous;
        }

        string? First(string type) => principal.FindFirst(type)?.Value;

        var claims = principal.Claims
            .GroupBy(c => c.Type, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Count() == 1
                    ? JsonSerializer.SerializeToElement(g.First().Value)
                    : JsonSerializer.SerializeToElement(g.Select(c => c.Value).ToArray()),
                StringComparer.Ordinal);

        return new AppUser
        {
            Id = First(AppClaims.Subject) ?? principal.Identity.Name ?? string.Empty,
            IsAuthenticated = true,
            Name = First(AppClaims.Name),
            Email = First(AppClaims.Email),
            Username = First(AppClaims.PreferredUsername),
            Picture = First(AppClaims.Picture),
            Scopes = ScopeAuthorizationHandler.ReadScopes(principal),
            Claims = claims,
        };
    }
}
