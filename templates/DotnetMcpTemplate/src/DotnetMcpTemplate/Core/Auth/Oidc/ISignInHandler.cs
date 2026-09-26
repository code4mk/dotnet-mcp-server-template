using System.Text.Json.Nodes;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>
/// Runs once per sign-in (AUTH_PROVIDER=oidc), after the user signed in at the identity provider and before the MCP
/// client gets a token: sync the user to your backend, fetch permissions, check a subscription, or refuse the sign-in.
/// Register as many as you like, in order: <c>services.AddScoped&lt;ISignInHandler, MySignInHandler&gt;()</c>. Each runs
/// in a DI scope, so it can use typed API clients and scoped services.
/// </summary>
/// <remarks>
/// Data put in <see cref="SignInContext.SessionData"/> is kept with the session and handed to every
/// <see cref="ITokenClaimsEnricher"/>, at sign-in and on each refresh, without calling your backend again. An exception
/// fails the sign-in with a generic error (and is logged); use <see cref="SignInContext.Deny"/> for expected refusals.
/// </remarks>
public interface ISignInHandler
{
    ValueTask OnSignInAsync(SignInContext context, CancellationToken cancellationToken);
}

/// <summary>What an <see cref="ISignInHandler"/> sees and decides.</summary>
public sealed class SignInContext(
    string subject,
    string issuer,
    string clientId,
    IReadOnlyList<string> scopes,
    JsonObject idToken,
    JsonObject? userInfo,
    JsonObject identityProviderClaims,
    string? identityProviderAccessToken)
{
    /// <summary>The user's id at the identity provider (sub).</summary>
    public string Subject { get; } = subject;

    /// <summary>The identity provider (iss of the ID token).</summary>
    public string Issuer { get; } = issuer;

    /// <summary>The MCP client the user is signing in to.</summary>
    public string ClientId { get; } = clientId;

    /// <summary>Scopes the client will be granted.</summary>
    public IReadOnlyList<string> Scopes { get; } = scopes;

    /// <summary>The validated ID token's claims, exactly as the IdP sent them (including iss, aud, exp).</summary>
    public JsonObject IdToken { get; } = idToken;

    /// <summary>The userinfo response, or null when it wasn't called (OIDC_USERINFO, or no userinfo endpoint).</summary>
    public JsonObject? UserInfo { get; } = userInfo;

    /// <summary>ID token and userinfo combined: userinfo wins for the same claim, protocol claims removed.</summary>
    public JsonObject IdentityProviderClaims { get; } = identityProviderClaims;

    /// <summary>
    /// The IdP access token (OIDC_SCOPES decides what it grants). Use it to call an API that accepts your IdP's tokens
    /// as this user. Server-side only: never return it to a client or log it.
    /// </summary>
    public string? IdentityProviderAccessToken { get; } = identityProviderAccessToken;

    /// <summary>
    /// Kept with the session (JSON): permissions, your internal user id, a plan. Enrichers read it on every token.
    /// Keep it small, and don't store secrets here.
    /// </summary>
    public JsonObject SessionData { get; } = new();

    /// <summary>Set by <see cref="Deny"/>.</summary>
    public string? DeniedReason { get; private set; }

    /// <summary>Values of an IdP claim at a dot path (see <see cref="IdpClaims.Values"/>).</summary>
    public IReadOnlyList<string> IdpValues(string path) => IdpClaims.Values(IdentityProviderClaims, path);

    /// <summary>
    /// Refuses the sign-in: the MCP client receives <c>access_denied</c> with this message, and no session is created.
    /// </summary>
    public void Deny(string reason) => DeniedReason = string.IsNullOrWhiteSpace(reason) ? "Sign-in was refused." : reason;
}
