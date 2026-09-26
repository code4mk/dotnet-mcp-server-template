using System.ComponentModel.DataAnnotations;

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

    /// <summary>
    /// How long a user stays signed in without using the server. Every refresh starts the period again, so an active
    /// user never has to sign in again.
    /// </summary>
    [ConfigurationKeyName("AUTH_REFRESH_TOKEN_LIFETIME_DAYS")]
    [Range(1, 365)]
    public int RefreshTokenLifetimeDays { get; init; } = 30;

    /// <summary>
    /// Refresh tokens rotate (single use). For this many seconds after a refresh, the old refresh token returns the same
    /// new tokens again, so concurrent refreshes and retries after a lost response don't sign the user out. 0 = off.
    /// </summary>
    [ConfigurationKeyName("AUTH_REFRESH_REUSE_SECONDS")]
    [Range(0, 300)]
    public int RefreshReuseSeconds { get; init; } = 30;

    /// <summary>Remember consent per client for N days (0 = always ask).</summary>
    [ConfigurationKeyName("AUTH_CONSENT_REMEMBER_DAYS")]
    [Range(0, 365)]
    public int ConsentRememberDays { get; init; } = 30;

    /// <summary>How long an unused dynamic client registration is kept. Every token request extends it.</summary>
    [ConfigurationKeyName("AUTH_CLIENT_REGISTRATION_DAYS")]
    [Range(1, 3650)]
    public int ClientRegistrationDays { get; init; } = 90;

    /// <summary>
    /// Where sign-ins are kept. file: on disk, survives restarts (one instance). redis: shared by several instances,
    /// through the app's Redis connection (REDIS_URL or your IRedisConnectionFactory, see Core/Redis).
    /// memory: lost on every restart (tests).
    /// </summary>
    [ConfigurationKeyName("AUTH_STORE")]
    [AllowedValues("file", "redis", "memory")]
    public string Store { get; init; } = "file";

    /// <summary>Folder for AUTH_STORE=file, relative to the working directory. Keep it out of git and on a volume in Docker.</summary>
    [ConfigurationKeyName("AUTH_STORE_PATH")]
    public string StorePath { get; init; } = ".data/auth";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Store == "file" && string.IsNullOrWhiteSpace(StorePath))
        {
            yield return new ValidationResult("AUTH_STORE_PATH: required when AUTH_STORE=file.");
        }
    }
}
