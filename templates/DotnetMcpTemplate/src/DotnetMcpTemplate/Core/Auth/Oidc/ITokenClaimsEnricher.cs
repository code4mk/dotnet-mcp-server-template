using System.Text.Json;
using System.Text.Json.Nodes;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>
/// Shapes the claims of the server's access token (AUTH_PROVIDER=oidc). Runs every time a token is built: at sign-in
/// and on every refresh, so values you look up (roles from your database, a tenant, a plan) stay current within one
/// access-token lifetime. Register as many as you like, in order:
/// <c>services.AddScoped&lt;ITokenClaimsEnricher, MyEnricher&gt;()</c>. Each runs in a DI scope, so it can use
/// scoped services such as a DbContext. To call your backend once per sign-in instead, use an
/// <see cref="ISignInHandler"/> and read its <see cref="TokenClaimsContext.SessionData"/> here.
/// </summary>
/// <remarks>
/// Protected claims (<see cref="TokenClaimsContext.Protected"/>: sub, scope, client_id, sid, idp, jti) are restored
/// after the enrichers run: an enricher can't change who the user is or grant scopes.
/// </remarks>
public interface ITokenClaimsEnricher
{
    ValueTask EnrichAsync(TokenClaimsContext context, CancellationToken cancellationToken);
}

/// <summary>Why a token is being built.</summary>
public enum TokenIssue
{
    /// <summary>The first token after the user signed in.</summary>
    SignIn,

    /// <summary>A new token for an existing session (the client used its refresh token).</summary>
    Refresh,
}

/// <summary>What an <see cref="ITokenClaimsEnricher"/> sees and changes.</summary>
public sealed class TokenClaimsContext(
    TokenIssue reason,
    string subject,
    string clientId,
    IReadOnlyList<string> scopes,
    JsonObject? idToken,
    JsonObject? userInfo,
    JsonObject identityProviderClaims,
    JsonObject sessionData,
    IDictionary<string, object> claims)
{
    /// <summary>Claims an enricher can't change.</summary>
    public static IReadOnlySet<string> Protected { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        AppClaims.Subject, AppClaims.Scope, AppClaims.ClientId, AppClaims.SessionId, AppClaims.IdentityProvider, "jti",
    };

    /// <summary>Sign-in or refresh.</summary>
    public TokenIssue Reason { get; } = reason;

    /// <summary>The user's id at the identity provider (sub).</summary>
    public string Subject { get; } = subject;

    /// <summary>The MCP client the token is for.</summary>
    public string ClientId { get; } = clientId;

    /// <summary>The granted scopes (read-only here).</summary>
    public IReadOnlyList<string> Scopes { get; } = scopes;

    /// <summary>The validated ID token's claims from sign-in, exactly as the IdP sent them.</summary>
    public JsonObject? IdToken { get; } = idToken;

    /// <summary>The userinfo response from sign-in, or null when it wasn't called.</summary>
    public JsonObject? UserInfo { get; } = userInfo;

    /// <summary>ID token and userinfo combined: userinfo wins for the same claim, protocol claims removed.</summary>
    public JsonObject IdentityProviderClaims { get; } = identityProviderClaims;

    /// <summary>What <see cref="ISignInHandler"/>s stored at sign-in (read-only here); empty when none did.</summary>
    public JsonObject SessionData { get; } = sessionData;

    /// <summary>
    /// The claims going into the token, by name. Values: string, number, bool, string[] (one claim per value, so
    /// <c>RequireClaim("roles", "admin")</c> works) or <see cref="JsonElement"/>.
    /// </summary>
    public IDictionary<string, object> Claims { get; } = claims;

    /// <summary>Sets a claim; an array or list becomes one claim per value.</summary>
    public void Set(string name, object value) => Claims[name] = value;

    public void Remove(string name) => Claims.Remove(name);

    /// <summary>Values of a combined IdP claim at a dot path (see <see cref="IdpClaims.Values"/>).</summary>
    public IReadOnlyList<string> IdpValues(string path) => IdpClaims.Values(IdentityProviderClaims, path);

    /// <summary>Values stored by a sign-in handler at a dot path, e.g. <c>SessionValues("permissions")</c>.</summary>
    public IReadOnlyList<string> SessionValues(string path) => IdpClaims.Values(SessionData, path);
}
