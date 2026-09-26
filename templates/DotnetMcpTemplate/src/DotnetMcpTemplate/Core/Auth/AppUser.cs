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

    public IReadOnlyList<string> Roles { get; init; } = [];

    public IReadOnlyList<string> Groups { get; init; } = [];

    public IReadOnlyList<string> Scopes { get; init; } = [];

    /// <summary>Every claim of the token (arrays for repeated claims).</summary>
    public IReadOnlyDictionary<string, JsonElement> Claims { get; init; } = new Dictionary<string, JsonElement>();

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public bool HasScope(string scope) => Scopes.Contains(scope, StringComparer.Ordinal);

    public static AppUser FromPrincipal(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return Anonymous;
        }

        string? First(string type) => principal.FindFirst(type)?.Value;
        IReadOnlyList<string> All(string type) => principal.FindAll(type).Select(c => c.Value).Distinct(StringComparer.Ordinal).ToArray();

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
            Roles = All(AppClaims.Roles),
            Groups = All(AppClaims.Groups),
            Scopes = ScopeAuthorizationHandler.ReadScopes(principal),
            Claims = claims,
        };
    }
}
