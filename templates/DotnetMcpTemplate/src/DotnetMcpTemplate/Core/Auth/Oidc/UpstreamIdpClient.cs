using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>Talks to the identity provider: code exchange, ID token validation, userinfo and refresh.</summary>
public sealed class UpstreamIdpClient(
    OidcSettings settings,
    OidcDiscovery discovery,
    IHttpClientFactory httpClientFactory,
    ILogger<UpstreamIdpClient> logger)
{
    private readonly JsonWebTokenHandler _handler = new();

    public Task<UpstreamTokenResponse> ExchangeCodeAsync(string code, string redirectUri, string codeVerifier, CancellationToken cancellationToken) =>
        TokenRequestAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier,
        }, cancellationToken);

    public Task<UpstreamTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken) =>
        TokenRequestAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        }, cancellationToken);

    /// <summary>Validates signature (JWKS), issuer, audience (our client id), lifetime and nonce. Returns the claims.</summary>
    public async Task<JsonObject> ValidateIdTokenAsync(string idToken, string expectedNonce, CancellationToken cancellationToken)
    {
        var result = await ValidateOnceAsync(idToken, cancellationToken);
        if (!result.IsValid && result.Exception is SecurityTokenSignatureKeyNotFoundException)
        {
            discovery.RequestRefresh();   // the IdP may have rotated its keys
            result = await ValidateOnceAsync(idToken, cancellationToken);
        }

        if (!result.IsValid)
        {
            logger.LogWarning(result.Exception, "ID token validation failed");
            throw new OAuthException("server_error", "The identity provider returned an invalid ID token.");
        }

        var token = (JsonWebToken)result.SecurityToken;
        var claims = JsonNode.Parse(Base64Url.DecodeFromChars(token.EncodedPayload))!.AsObject();

        var nonce = claims["nonce"]?.ToString();
        if (nonce is null || !AuthCrypto.FixedTimeEquals(nonce, expectedNonce))
        {
            throw new OAuthException("server_error", "The ID token nonce does not match.");
        }

        return claims;
    }

    /// <summary>Calls userinfo when configured (OIDC_USERINFO) and advertised by the IdP.</summary>
    public async Task<JsonObject?> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        if (settings.UserInfo == "never")
        {
            return null;
        }

        var configuration = await discovery.GetAsync(cancellationToken);
        if (string.IsNullOrEmpty(configuration.UserInfoEndpoint))
        {
            return settings.UserInfo == "always"
                ? throw new OAuthException("server_error", "OIDC_USERINFO=always but the IdP has no userinfo endpoint.")
                : null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, configuration.UserInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await Http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Userinfo returned {Status}", (int)response.StatusCode);
            if (settings.UserInfo == "always")
            {
                throw new OAuthException("server_error", "The identity provider's userinfo request failed.");
            }

            return null;
        }

        // Signed/encrypted userinfo (application/jwt) isn't supported; most IdPs return JSON.
        return await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken);
    }

    private HttpClient Http => httpClientFactory.CreateClient(OidcAuthProvider.HttpClientName);

    private async Task<TokenValidationResult> ValidateOnceAsync(string idToken, CancellationToken cancellationToken)
    {
        var configuration = await discovery.GetAsync(cancellationToken);
        return await _handler.ValidateTokenAsync(idToken, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = configuration.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.ClientId,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ClockSkew = TimeSpan.FromMinutes(2),
        });
    }

    private async Task<UpstreamTokenResponse> TokenRequestAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        var configuration = await discovery.GetAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, configuration.TokenEndpoint);

        if (settings.HasClientSecret && settings.ClientAuthMethod == "client_secret_basic")
        {
            var credentials = Uri.EscapeDataString(settings.ClientId) + ":" + Uri.EscapeDataString(settings.ClientSecret!);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));
        }
        else
        {
            form["client_id"] = settings.ClientId;
            if (settings.HasClientSecret)
            {
                form["client_secret"] = settings.ClientSecret!;
            }
        }

        request.Content = new FormUrlEncodedContent(form);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await Http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("IdP token endpoint returned {Status}: {Body}", (int)response.StatusCode, body.Length > 500 ? body[..500] : body);
            throw new OAuthException("server_error", "The identity provider rejected the token request.");
        }

        return await response.Content.ReadFromJsonAsync<UpstreamTokenResponse>(cancellationToken)
            ?? throw new OAuthException("server_error", "The identity provider returned an empty token response.");
    }
}
