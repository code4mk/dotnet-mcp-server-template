using System.ComponentModel.DataAnnotations;
using DotnetMcpTemplate.Core.Common.Settings;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>The OAuth proxy: this server acts as the OAuth authorization server for MCP clients.</summary>
public sealed class OidcProxySettings : IValidatableObject
{
    /// <summary>Signs server tokens and encrypts stored IdP tokens. Rotating it signs everyone out.</summary>
    [ConfigurationKeyName("AUTH_TOKEN_SIGNING_KEY")]
    [Required, MinLength(32)]
    public string TokenSigningKey { get; init; } = string.Empty;

    [ConfigurationKeyName("AUTH_TOKEN_LIFETIME_MINUTES")]
    [Range(1, 1440)]
    public int TokenLifetimeMinutes { get; init; } = 60;

    [ConfigurationKeyName("AUTH_REFRESH_TOKEN_LIFETIME_DAYS")]
    [Range(1, 365)]
    public int RefreshTokenLifetimeDays { get; init; } = 30;

    /// <summary>Redirect URIs MCP clients may register, comma-separated. * is allowed in port and path.</summary>
    [ConfigurationKeyName("AUTH_ALLOWED_REDIRECT_URIS")]
    [Required]
    public string AllowedRedirectUris { get; init; } = "http://localhost:*,http://127.0.0.1:*";

    /// <summary>Remember consent per client for N days (0 = always ask).</summary>
    [ConfigurationKeyName("AUTH_CONSENT_REMEMBER_DAYS")]
    [Range(0, 365)]
    public int ConsentRememberDays { get; init; } = 30;

    /// <summary>How long a dynamic client registration is kept.</summary>
    [ConfigurationKeyName("AUTH_CLIENT_REGISTRATION_DAYS")]
    [Range(1, 3650)]
    public int ClientRegistrationDays { get; init; } = 90;

    /// <summary>memory (single instance) | redis (several instances share logins).</summary>
    [ConfigurationKeyName("AUTH_STORE")]
    [AllowedValues("memory", "redis")]
    public string Store { get; init; } = "memory";

    [ConfigurationKeyName("REDIS_URL")]
    public string? RedisUrl { get; init; }

    public IReadOnlyList<string> RedirectUriPatterns => AllowedRedirectUris.SplitList();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Store == "redis" && string.IsNullOrWhiteSpace(RedisUrl))
        {
            yield return new ValidationResult("REDIS_URL: required when AUTH_STORE=redis.");
        }
    }
}
