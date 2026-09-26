using System.ComponentModel.DataAnnotations;
using DotnetMcpTemplate.Core.Common.Settings;

namespace DotnetMcpTemplate.Core.ApiClients;

public static class ApiAuthTypes
{
    public const string None = "none";
    public const string Bearer = "bearer";
    public const string ApiKey = "api_key";
    public const string Basic = "basic";
    public const string ClientCredentials = "client_credentials";

    /// <summary>The signed-in user's IdP access token (oidc provider only).</summary>
    public const string User = "user";
}

/// <summary>
/// Settings of one data source, read from environment variables with a prefix, e.g. prefix CRM:
/// CRM_BASE_URL, CRM_TIMEOUT_SECONDS, CRM_AUTH, CRM_API_KEY, ... Validated at startup with the variable names.
/// </summary>
public sealed class ApiClientSettings : IValidatableObject
{
    [ConfigurationKeyName("_BASE_URL")]
    [Required, Url]
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>Timeout per attempt.</summary>
    [ConfigurationKeyName("_TIMEOUT_SECONDS")]
    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>Retries for transient failures (5xx, 408, 429, network errors).</summary>
    [ConfigurationKeyName("_RETRY_COUNT")]
    [Range(1, 10)]
    public int RetryCount { get; init; } = 3;

    /// <summary>Also retry POST/PATCH/PUT/DELETE. Only enable for idempotent APIs.</summary>
    [ConfigurationKeyName("_RETRY_UNSAFE_METHODS")]
    public bool RetryUnsafeMethods { get; init; }

    /// <summary>Default headers: <c>Name=Value;Other=Value</c>.</summary>
    [ConfigurationKeyName("_HEADERS")]
    public string? Headers { get; init; }

    [ConfigurationKeyName("_AUTH")]
    [AllowedValues(ApiAuthTypes.None, ApiAuthTypes.Bearer, ApiAuthTypes.ApiKey, ApiAuthTypes.Basic, ApiAuthTypes.ClientCredentials, ApiAuthTypes.User)]
    public string Auth { get; init; } = ApiAuthTypes.None;

    [ConfigurationKeyName("_TOKEN")]
    public string? Token { get; init; }

    [ConfigurationKeyName("_API_KEY")]
    public string? ApiKey { get; init; }

    [ConfigurationKeyName("_API_KEY_HEADER")]
    public string ApiKeyHeader { get; init; } = "X-Api-Key";

    [ConfigurationKeyName("_USERNAME")]
    public string? Username { get; init; }

    [ConfigurationKeyName("_PASSWORD")]
    public string? Password { get; init; }

    [ConfigurationKeyName("_TOKEN_URL")]
    public string? TokenUrl { get; init; }

    [ConfigurationKeyName("_CLIENT_ID")]
    public string? ClientId { get; init; }

    [ConfigurationKeyName("_CLIENT_SECRET")]
    public string? ClientSecret { get; init; }

    [ConfigurationKeyName("_SCOPE")]
    public string? Scope { get; init; }

    /// <summary>Set by the registration: the env prefix, e.g. SAMPLE_API.</summary>
    public string Prefix { get; internal set; } = string.Empty;

    public IReadOnlyDictionary<string, string> DefaultHeaders =>
        (Headers ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(pair => pair.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2 && parts[0].Length > 0)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        string Key(string suffix) => Prefix + suffix;

        switch (Auth)
        {
            case ApiAuthTypes.Bearer when string.IsNullOrWhiteSpace(Token):
                yield return new($"{Key("_TOKEN")}: required when {Key("_AUTH")}=bearer.");
                break;
            case ApiAuthTypes.ApiKey when string.IsNullOrWhiteSpace(ApiKey):
                yield return new($"{Key("_API_KEY")}: required when {Key("_AUTH")}=api_key.");
                break;
            case ApiAuthTypes.Basic when string.IsNullOrWhiteSpace(Username):
                yield return new($"{Key("_USERNAME")}: required when {Key("_AUTH")}=basic.");
                break;
            case ApiAuthTypes.ClientCredentials:
                if (!Uri.TryCreate(TokenUrl, UriKind.Absolute, out _))
                {
                    yield return new($"{Key("_TOKEN_URL")}: an absolute URL is required when {Key("_AUTH")}=client_credentials.");
                }

                if (string.IsNullOrWhiteSpace(ClientId) || string.IsNullOrWhiteSpace(ClientSecret))
                {
                    yield return new($"{Key("_CLIENT_ID")} and {Key("_CLIENT_SECRET")}: required when {Key("_AUTH")}=client_credentials.");
                }

                break;
        }
    }
}
