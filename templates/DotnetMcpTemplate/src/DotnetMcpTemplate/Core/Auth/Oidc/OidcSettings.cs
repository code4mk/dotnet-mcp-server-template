using System.ComponentModel.DataAnnotations;
using DotnetMcpTemplate.Core.Common.Settings;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>Your identity provider (any OIDC IdP). Registered only when AUTH_PROVIDER=oidc.</summary>
public sealed class OidcSettings : IValidatableObject
{
    /// <summary>The IdP's discovery document, e.g. https://login.example.com/.well-known/openid-configuration.</summary>
    [ConfigurationKeyName("OIDC_DISCOVERY_URL")]
    [Required, Url]
    public string DiscoveryUrl { get; init; } = string.Empty;

    [ConfigurationKeyName("OIDC_CLIENT_ID")]
    [Required]
    public string ClientId { get; init; } = string.Empty;

    /// <summary>Empty = public client (PKCE only).</summary>
    [ConfigurationKeyName("OIDC_CLIENT_SECRET")]
    public string? ClientSecret { get; init; }

    [ConfigurationKeyName("OIDC_CLIENT_AUTH_METHOD")]
    [AllowedValues("client_secret_basic", "client_secret_post")]
    public string ClientAuthMethod { get; init; } = "client_secret_basic";

    /// <summary>Scopes requested from the IdP, comma-separated. Must include openid.</summary>
    [ConfigurationKeyName("OIDC_SCOPES")]
    [Required]
    public string Scopes { get; init; } = "openid,profile,email";

    /// <summary>auto: call userinfo when the IdP advertises it | always | never.</summary>
    [ConfigurationKeyName("OIDC_USERINFO")]
    [AllowedValues("auto", "always", "never")]
    public string UserInfo { get; init; } = "auto";

    /// <summary>
    /// Extra IdP claims copied into server tokens, comma-separated (e.g. roles,groups,tenant_id). IdPs name these
    /// differently, so none are copied by default; read them from <see cref="AppUser.Claims"/> or require them in a policy.
    /// </summary>
    [ConfigurationKeyName("OIDC_TOKEN_CLAIMS")]
    public string? TokenClaims { get; init; }

    public IReadOnlyList<string> ScopeList => Scopes.SplitList();

    public bool HasClientSecret => !string.IsNullOrWhiteSpace(ClientSecret);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ScopeList.Contains("openid", StringComparer.Ordinal))
        {
            yield return new ValidationResult("OIDC_SCOPES: must include openid.");
        }
    }
}
