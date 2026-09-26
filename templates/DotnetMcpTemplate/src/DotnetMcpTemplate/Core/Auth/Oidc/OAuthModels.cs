using System.Text.Json.Serialization;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>A dynamically registered MCP client (RFC 7591).</summary>
public sealed record ClientRegistration(
    string ClientId,
    string? ClientSecretHash,
    string ClientName,
    IReadOnlyList<string> RedirectUris,
    string TokenEndpointAuthMethod,
    DateTimeOffset CreatedAt);

/// <summary>A login in progress: from /oauth/authorize until the IdP calls /oauth/callback.</summary>
public sealed record AuthorizationTransaction(
    string Id,
    string ClientId,
    string RedirectUri,
    string? ClientState,
    string CodeChallenge,
    IReadOnlyList<string> Scopes,
    string CsrfToken,
    string? UpstreamCodeVerifier = null,
    string? UpstreamNonce = null);

/// <summary>An authorization code issued to the MCP client (single use, 5 minutes).</summary>
public sealed record AuthorizationCodeEntry(
    string ClientId,
    string RedirectUri,
    string CodeChallenge,
    string SessionId,
    IReadOnlyList<string> Scopes);

/// <summary>A signed-in user: merged identity plus the encrypted IdP tokens.</summary>
public sealed record UserSession(
    string Id,
    string Subject,
    string ClientId,
    string Issuer,
    string MergedClaimsJson,
    string EncryptedUpstreamTokens,
    DateTimeOffset CreatedAt);

public sealed record RefreshTokenEntry(string SessionId, string ClientId, IReadOnlyList<string> Scopes);

/// <summary>IdP tokens kept on the server (never sent to MCP clients).</summary>
public sealed record UpstreamTokens(string AccessToken, string? RefreshToken, DateTimeOffset? ExpiresAt);

/// <summary>RFC 7591 registration request (only the fields we use).</summary>
public sealed class RegistrationRequest
{
    [JsonPropertyName("redirect_uris")]
    public List<string>? RedirectUris { get; set; }

    [JsonPropertyName("client_name")]
    public string? ClientName { get; set; }

    [JsonPropertyName("token_endpoint_auth_method")]
    public string? TokenEndpointAuthMethod { get; set; }

    [JsonPropertyName("grant_types")]
    public List<string>? GrantTypes { get; set; }

    [JsonPropertyName("response_types")]
    public List<string>? ResponseTypes { get; set; }
}

/// <summary>Token endpoint response from the IdP.</summary>
public sealed class UpstreamTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("id_token")]
    public string? IdToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }
}

public class OAuthException(string error, string description, int statusCode = 400) : Exception(description)
{
    public string Error { get; } = error;

    public int StatusCode { get; } = statusCode;
}
