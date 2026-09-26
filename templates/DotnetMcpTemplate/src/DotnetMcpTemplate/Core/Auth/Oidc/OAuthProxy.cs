using System.Text.Json;
using System.Text.Json.Nodes;
using DotnetMcpTemplate.Core.Common.Settings;
using DotnetMcpTemplate.Core.Mcp;
using Microsoft.AspNetCore.WebUtilities;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>
/// The OAuth 2.1 authorization server MCP clients talk to. It registers clients (RFC 7591), asks the user for
/// consent, logs them in at the upstream IdP (authorization code + PKCE), and issues this server's tokens.
/// PKCE S256 is required on both legs; codes and refresh tokens are single use; refresh tokens rotate.
/// </summary>
public sealed class OAuthProxy(
    AuthStore store,
    AuthCrypto crypto,
    OidcSettings oidc,
    OidcProxySettings proxy,
    AppSettings app,
    McpSettings mcp,
    OidcDiscovery discovery,
    UpstreamIdpClient idp,
    ClaimsMerger merger,
    TokenIssuer issuer,
    RedirectUriPolicy redirectPolicy,
    TimeProvider time,
    ILogger<OAuthProxy> logger)
{
    public const string AuthorizePath = "/oauth/authorize";
    public const string ConsentPath = "/oauth/consent";
    public const string CallbackPath = "/oauth/callback";
    public const string TokenPath = "/oauth/token";
    public const string RegisterPath = "/oauth/register";

    private static readonly TimeSpan TransactionLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(5);
    private static readonly string[] AuthMethods = ["none", "client_secret_post", "client_secret_basic"];

    public string Issuer => app.PublicUrl;

    public string ResourceUrl => app.PublicUrl + mcp.NormalizedPath;

    /// <summary>MCP_SERVER_NAME, shown on the consent page.</summary>
    public string ServerName => mcp.ServerName;

    public string CallbackUrl => app.PublicUrl + CallbackPath;

    /// <summary>RFC 8414 authorization server metadata.</summary>
    public object Metadata() => new Dictionary<string, object>
    {
        ["issuer"] = Issuer,
        ["authorization_endpoint"] = app.PublicUrl + AuthorizePath,
        ["token_endpoint"] = app.PublicUrl + TokenPath,
        ["registration_endpoint"] = app.PublicUrl + RegisterPath,
        ["response_types_supported"] = new[] { "code" },
        ["response_modes_supported"] = new[] { "query" },
        ["grant_types_supported"] = new[] { "authorization_code", "refresh_token" },
        ["code_challenge_methods_supported"] = new[] { "S256" },
        ["token_endpoint_auth_methods_supported"] = AuthMethods,
        ["scopes_supported"] = AppScopes.All,
        ["authorization_response_iss_parameter_supported"] = true,
    };

    // ------------------------------------------------------------------ registration (RFC 7591)

    public async Task<object> RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken)
    {
        var redirectUris = request.RedirectUris ?? [];
        if (redirectUris.Count == 0)
        {
            throw new OAuthException("invalid_redirect_uri", "redirect_uris is required.");
        }

        var rejected = redirectUris.FirstOrDefault(uri => !redirectPolicy.IsAllowed(uri));
        if (rejected is not null)
        {
            throw new OAuthException("invalid_redirect_uri", $"Redirect URI '{rejected}' is not allowed. {RedirectUriPolicy.Rules}");
        }

        var method = request.TokenEndpointAuthMethod ?? "client_secret_basic";   // RFC 7591 default
        if (!AuthMethods.Contains(method, StringComparer.Ordinal))
        {
            throw new OAuthException("invalid_client_metadata", $"token_endpoint_auth_method '{method}' is not supported.");
        }

        if (request.GrantTypes is { } grants && grants.Any(g => g is not ("authorization_code" or "refresh_token")))
        {
            throw new OAuthException("invalid_client_metadata", "Only authorization_code and refresh_token grants are supported.");
        }

        var clientName = string.IsNullOrWhiteSpace(request.ClientName) ? "MCP client" : request.ClientName.Trim();
        if (clientName.Length > 100)
        {
            clientName = clientName[..100];
        }

        var clientId = "mcp_" + AuthCrypto.RandomToken(18);
        var secret = method == "none" ? null : AuthCrypto.RandomToken(32);
        var registration = new ClientRegistration(
            clientId, secret is null ? null : AuthCrypto.Hash(secret), clientName, redirectUris, method, time.GetUtcNow());

        await store.SetAsync(AuthStore.ClientKey(clientId), registration, TimeSpan.FromDays(proxy.ClientRegistrationDays), cancellationToken);
        logger.LogInformation("Registered MCP client {ClientName} ({ClientId})", clientName, clientId);

        var response = new Dictionary<string, object>
        {
            ["client_id"] = clientId,
            ["client_id_issued_at"] = registration.CreatedAt.ToUnixTimeSeconds(),
            ["client_name"] = clientName,
            ["redirect_uris"] = redirectUris,
            ["grant_types"] = new[] { "authorization_code", "refresh_token" },
            ["response_types"] = new[] { "code" },
            ["token_endpoint_auth_method"] = method,
        };
        if (secret is not null)
        {
            response["client_secret"] = secret;
            response["client_secret_expires_at"] = 0;
        }

        return response;
    }

    // ------------------------------------------------------------------ authorize → consent → IdP

    /// <summary>Validates the request. Errors before the redirect URI is trusted are shown on a page, never redirected.</summary>
    public async Task<AuthorizationTransaction> BeginAsync(IQueryCollection query, CancellationToken cancellationToken)
    {
        var clientId = query["client_id"].ToString();
        var client = await store.GetAsync<ClientRegistration>(AuthStore.ClientKey(clientId), cancellationToken)
            ?? throw new OAuthException("invalid_client", "Unknown client_id. The client must register first.");

        var redirectUri = query["redirect_uri"].ToString();
        if (string.IsNullOrEmpty(redirectUri) && client.RedirectUris.Count == 1)
        {
            redirectUri = client.RedirectUris[0];
        }

        if (!client.RedirectUris.Contains(redirectUri, StringComparer.Ordinal))
        {
            throw new OAuthException("invalid_request", "redirect_uri does not match the registered redirect URIs.");
        }

        var state = query["state"].ToString();
        OAuthException Fail(string error, string description) => new RedirectableOAuthException(error, description, redirectUri, state);

        if (query["response_type"] != "code")
        {
            throw Fail("unsupported_response_type", "response_type must be code.");
        }

        var challenge = query["code_challenge"].ToString();
        if (string.IsNullOrEmpty(challenge) || query["code_challenge_method"] != "S256")
        {
            throw Fail("invalid_request", "PKCE with code_challenge_method=S256 is required.");
        }

        var resource = query["resource"].ToString();
        if (!string.IsNullOrEmpty(resource) && !SameUrl(resource, ResourceUrl))
        {
            throw Fail("invalid_target", $"This server only issues tokens for {ResourceUrl}.");
        }

        var requested = query["scope"].ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        // Only the requested scopes this server knows; no scope requested = signed in, no extra permissions.
        var granted = requested.Where(s => AppScopes.All.Contains(s, StringComparer.Ordinal)).Distinct().ToArray();

        var transaction = new AuthorizationTransaction(
            AuthCrypto.RandomToken(), client.ClientId, redirectUri, string.IsNullOrEmpty(state) ? null : state,
            challenge, granted, AuthCrypto.RandomToken());

        await store.SetAsync(AuthStore.TransactionKey(transaction.Id), transaction, TransactionLifetime, cancellationToken);
        return transaction;
    }

    public Task<ClientRegistration?> GetClientAsync(string clientId, CancellationToken cancellationToken) =>
        store.GetAsync<ClientRegistration>(AuthStore.ClientKey(clientId), cancellationToken);

    public Task<AuthorizationTransaction?> GetTransactionAsync(string id, CancellationToken cancellationToken) =>
        store.GetAsync<AuthorizationTransaction>(AuthStore.TransactionKey(id), cancellationToken);

    /// <summary>Starts the upstream login: PKCE + nonce, then the IdP authorization URL.</summary>
    public async Task<string> StartUpstreamLoginAsync(AuthorizationTransaction transaction, CancellationToken cancellationToken)
    {
        var configuration = await discovery.GetAsync(cancellationToken);
        var verifier = AuthCrypto.RandomToken(48);
        var nonce = AuthCrypto.RandomToken(24);

        await store.SetAsync(AuthStore.TransactionKey(transaction.Id),
            transaction with { UpstreamCodeVerifier = verifier, UpstreamNonce = nonce }, TransactionLifetime, cancellationToken);

        return QueryHelpers.AddQueryString(configuration.AuthorizationEndpoint, new Dictionary<string, string?>
        {
            ["client_id"] = oidc.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = CallbackUrl,
            ["scope"] = string.Join(' ', oidc.ScopeList),
            ["state"] = transaction.Id,
            ["nonce"] = nonce,
            ["code_challenge"] = AuthCrypto.PkceChallenge(verifier),
            ["code_challenge_method"] = "S256",
        });
    }

    public string DenyRedirect(AuthorizationTransaction transaction) =>
        ErrorRedirect(transaction.RedirectUri, "access_denied", "The user denied the request.", transaction.ClientState);

    // ------------------------------------------------------------------ IdP callback → code for the MCP client

    public async Task<string> CompleteUpstreamLoginAsync(IQueryCollection query, CancellationToken cancellationToken)
    {
        var transaction = await store.TakeAsync<AuthorizationTransaction>(AuthStore.TransactionKey(query["state"].ToString()), cancellationToken)
            ?? throw new OAuthException("invalid_request", "The login expired or was already used. Start again from your MCP client.");

        if (!string.IsNullOrEmpty(query["error"]))
        {
            logger.LogInformation("IdP returned {Error}: {Description}", query["error"].ToString(), query["error_description"].ToString());
            return ErrorRedirect(transaction.RedirectUri, "access_denied", "Sign-in at the identity provider did not complete.", transaction.ClientState);
        }

        if (transaction.UpstreamCodeVerifier is null || transaction.UpstreamNonce is null)
        {
            throw new OAuthException("invalid_request", "The login was not started correctly.");
        }

        var tokens = await idp.ExchangeCodeAsync(query["code"].ToString(), CallbackUrl, transaction.UpstreamCodeVerifier, cancellationToken);
        if (string.IsNullOrEmpty(tokens.IdToken))
        {
            throw new OAuthException("server_error", "The identity provider returned no ID token (is the openid scope requested?).");
        }

        var idClaims = await idp.ValidateIdTokenAsync(tokens.IdToken, transaction.UpstreamNonce, cancellationToken);
        var userInfo = tokens.AccessToken is null ? null : await idp.GetUserInfoAsync(tokens.AccessToken, cancellationToken);
        var merged = merger.Merge(idClaims, userInfo);

        var upstream = new UpstreamTokens(
            tokens.AccessToken ?? string.Empty,
            tokens.RefreshToken,
            tokens.ExpiresIn is { } seconds ? time.GetUtcNow().AddSeconds(seconds) : null);

        var session = new UserSession(
            Id: AuthCrypto.RandomToken(18),
            Subject: merged["sub"]!.ToString(),
            ClientId: transaction.ClientId,
            Issuer: idClaims["iss"]?.ToString() ?? string.Empty,
            MergedClaimsJson: merged.ToJsonString(),
            EncryptedUpstreamTokens: crypto.Encrypt(JsonSerializer.Serialize(upstream)),
            CreatedAt: time.GetUtcNow());
        await store.SetAsync(AuthStore.SessionKey(session.Id), session, TimeSpan.FromDays(proxy.RefreshTokenLifetimeDays), cancellationToken);

        var code = AuthCrypto.RandomToken();
        await store.SetAsync(AuthStore.CodeKey(code),
            new AuthorizationCodeEntry(transaction.ClientId, transaction.RedirectUri, transaction.CodeChallenge, session.Id, transaction.Scopes),
            CodeLifetime, cancellationToken);

        logger.LogInformation("User {Subject} signed in for client {ClientId}", session.Subject, session.ClientId);

        return QueryHelpers.AddQueryString(transaction.RedirectUri, new Dictionary<string, string?>
        {
            ["code"] = code,
            ["state"] = transaction.ClientState,
            ["iss"] = Issuer,   // RFC 9207
        }.Where(p => p.Value is not null));
    }

    // ------------------------------------------------------------------ token endpoint

    public async Task<object> TokenAsync(IFormCollection form, string? authorizationHeader, CancellationToken cancellationToken)
    {
        var client = await AuthenticateClientAsync(form, authorizationHeader, cancellationToken);

        return form["grant_type"].ToString() switch
        {
            "authorization_code" => await ExchangeCodeAsync(client, form, cancellationToken),
            "refresh_token" => await RefreshAsync(client, form, cancellationToken),
            _ => throw new OAuthException("unsupported_grant_type", "Use authorization_code or refresh_token."),
        };
    }

    private async Task<object> ExchangeCodeAsync(ClientRegistration client, IFormCollection form, CancellationToken cancellationToken)
    {
        var entry = await store.TakeAsync<AuthorizationCodeEntry>(AuthStore.CodeKey(form["code"].ToString()), cancellationToken)
            ?? throw new OAuthException("invalid_grant", "The authorization code is invalid, expired or already used.");

        if (entry.ClientId != client.ClientId)
        {
            throw new OAuthException("invalid_grant", "The authorization code was issued to another client.");
        }

        var redirectUri = form["redirect_uri"].ToString();
        if (!string.IsNullOrEmpty(redirectUri) && redirectUri != entry.RedirectUri)
        {
            throw new OAuthException("invalid_grant", "redirect_uri does not match the authorization request.");
        }

        var verifier = form["code_verifier"].ToString();
        if (string.IsNullOrEmpty(verifier) || !AuthCrypto.FixedTimeEquals(AuthCrypto.PkceChallenge(verifier), entry.CodeChallenge))
        {
            throw new OAuthException("invalid_grant", "PKCE verification failed.");
        }

        var session = await store.GetAsync<UserSession>(AuthStore.SessionKey(entry.SessionId), cancellationToken)
            ?? throw new OAuthException("invalid_grant", "The sign-in session has expired.");

        await KeepClientAsync(client, cancellationToken);
        return await IssueAsync(session, client.ClientId, entry.Scopes, cancellationToken);
    }

    // Refresh tokens rotate: each one is used once and answered with a new pair. The session and the client
    // registration are extended on every refresh, so a user who keeps using the server never signs in again.
    private async Task<object> RefreshAsync(ClientRegistration client, IFormCollection form, CancellationToken cancellationToken)
    {
        var refreshToken = form["refresh_token"].ToString();
        var entry = await store.TakeAsync<RefreshTokenEntry>(AuthStore.RefreshKey(refreshToken), cancellationToken);
        if (entry is null)
        {
            return await ReplayRotationAsync(client, refreshToken, cancellationToken)
                ?? throw new OAuthException("invalid_grant", "The refresh token is invalid, expired or already used.");
        }

        if (entry.ClientId != client.ClientId)
        {
            throw new OAuthException("invalid_grant", "The refresh token was issued to another client.");
        }

        var session = await store.GetAsync<UserSession>(AuthStore.SessionKey(entry.SessionId), cancellationToken)
            ?? throw new OAuthException("invalid_grant", "The sign-in session has expired. Sign in again.");

        await store.SetAsync(AuthStore.SessionKey(session.Id), session, TimeSpan.FromDays(proxy.RefreshTokenLifetimeDays), cancellationToken);
        await KeepClientAsync(client, cancellationToken);
        var response = await IssueAsync(session, client.ClientId, entry.Scopes, cancellationToken);

        if (proxy.RefreshReuseSeconds > 0)
        {
            var rotated = new RotatedRefreshToken(client.ClientId, time.GetUtcNow(), crypto.Encrypt(JsonSerializer.Serialize(response)));
            await store.SetAsync(AuthStore.RotatedKey(refreshToken), rotated, TimeSpan.FromSeconds(proxy.RefreshReuseSeconds), cancellationToken);
        }

        return response;
    }

    /// <summary>The same answer again when a just-rotated refresh token comes back within AUTH_REFRESH_REUSE_SECONDS.</summary>
    private async Task<object?> ReplayRotationAsync(ClientRegistration client, string refreshToken, CancellationToken cancellationToken)
    {
        if (proxy.RefreshReuseSeconds <= 0 || string.IsNullOrEmpty(refreshToken))
        {
            return null;
        }

        var rotated = await store.GetAsync<RotatedRefreshToken>(AuthStore.RotatedKey(refreshToken), cancellationToken);
        if (rotated is null
            || rotated.ClientId != client.ClientId
            || time.GetUtcNow() - rotated.RotatedAt > TimeSpan.FromSeconds(proxy.RefreshReuseSeconds))
        {
            return null;
        }

        logger.LogInformation("Refresh token reused within the grace period by client {ClientId}; returning the same tokens", client.ClientId);
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(crypto.Decrypt(rotated.EncryptedResponse));
    }

    /// <summary>A registration in use never expires: each successful token request starts its lifetime again.</summary>
    private Task KeepClientAsync(ClientRegistration client, CancellationToken cancellationToken) =>
        store.SetAsync(AuthStore.ClientKey(client.ClientId), client, TimeSpan.FromDays(proxy.ClientRegistrationDays), cancellationToken);

    private async Task<object> IssueAsync(UserSession session, string clientId, IReadOnlyList<string> scopes, CancellationToken cancellationToken)
    {
        var accessToken = issuer.CreateAccessToken(session with { ClientId = clientId }, scopes);
        var refreshToken = AuthCrypto.RandomToken();
        await store.SetAsync(AuthStore.RefreshKey(refreshToken), new RefreshTokenEntry(session.Id, clientId, scopes),
            TimeSpan.FromDays(proxy.RefreshTokenLifetimeDays), cancellationToken);

        return new Dictionary<string, object>
        {
            ["access_token"] = accessToken,
            ["token_type"] = "Bearer",
            ["expires_in"] = (int)issuer.Lifetime.TotalSeconds,
            ["refresh_token"] = refreshToken,
            ["scope"] = string.Join(' ', scopes),
        };
    }

    private async Task<ClientRegistration> AuthenticateClientAsync(IFormCollection form, string? authorizationHeader, CancellationToken cancellationToken)
    {
        string? clientId = form["client_id"];
        string? secret = form["client_secret"];

        if (authorizationHeader?.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(authorizationHeader[6..].Trim()));
                var separator = decoded.IndexOf(':');
                if (separator > 0)
                {
                    clientId = Uri.UnescapeDataString(decoded[..separator]);
                    secret = Uri.UnescapeDataString(decoded[(separator + 1)..]);
                }
            }
            catch (FormatException)
            {
                throw new OAuthException("invalid_client", "Malformed Basic authorization header.", 401);
            }
        }

        var client = string.IsNullOrEmpty(clientId)
            ? null
            : await store.GetAsync<ClientRegistration>(AuthStore.ClientKey(clientId), cancellationToken);

        if (client is null)
        {
            throw new OAuthException("invalid_client", "Unknown client.", 401);
        }

        if (client.ClientSecretHash is not null
            && (string.IsNullOrEmpty(secret) || !AuthCrypto.FixedTimeEquals(AuthCrypto.Hash(secret), client.ClientSecretHash)))
        {
            throw new OAuthException("invalid_client", "Client authentication failed.", 401);
        }

        return client;
    }

    // ------------------------------------------------------------------ consent cookie

    public string ConsentCookieName(string clientId) => "mcp_consent_" + AuthCrypto.Hash(clientId)[..12];

    public string ConsentCookieValue(string clientId) => crypto.Sign("consent:" + clientId);

    public bool HasRememberedConsent(HttpRequest request, string clientId) =>
        proxy.ConsentRememberDays > 0
        && request.Cookies.TryGetValue(ConsentCookieName(clientId), out var value)
        && crypto.Verify("consent:" + clientId, value);

    public int ConsentRememberDays => proxy.ConsentRememberDays;

    private static string ErrorRedirect(string redirectUri, string error, string description, string? state) =>
        QueryHelpers.AddQueryString(redirectUri, new Dictionary<string, string?>
        {
            ["error"] = error,
            ["error_description"] = description,
            ["state"] = state,
        }.Where(p => p.Value is not null));

    private static bool SameUrl(string a, string b) =>
        string.Equals(a.TrimEnd('/'), b.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
}

/// <summary>An authorize error that can be sent back to the (already validated) redirect URI.</summary>
public sealed class RedirectableOAuthException(string error, string description, string redirectUri, string? state)
    : OAuthException(error, description)
{
    public string RedirectUri { get; } = redirectUri;

    public string? State { get; } = string.IsNullOrEmpty(state) ? null : state;
}
