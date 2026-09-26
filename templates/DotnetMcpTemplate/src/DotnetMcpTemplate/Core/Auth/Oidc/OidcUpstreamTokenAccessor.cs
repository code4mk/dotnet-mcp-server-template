using System.Collections.Concurrent;
using System.Text.Json;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>Returns the signed-in user's IdP access token from their session, refreshing it when it's about to expire.</summary>
internal sealed class OidcUpstreamTokenAccessor(
    IHttpContextAccessor httpContextAccessor,
    AuthStore store,
    AuthCrypto crypto,
    UpstreamIdpClient idp,
    OidcProxySettings proxy,
    TimeProvider time) : IUpstreamTokenAccessor
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.Ordinal);

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var sessionId = httpContextAccessor.HttpContext?.User.FindFirst(AppClaims.SessionId)?.Value;
        if (sessionId is null)
        {
            return null;
        }

        var gate = Locks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var session = await store.GetAsync<UserSession>(AuthStore.SessionKey(sessionId), cancellationToken);
            if (session is null)
            {
                return null;
            }

            var tokens = JsonSerializer.Deserialize<UpstreamTokens>(crypto.Decrypt(session.EncryptedUpstreamTokens))!;
            var expiresSoon = tokens.ExpiresAt is { } expiry && expiry <= time.GetUtcNow().AddSeconds(60);
            if (!expiresSoon || tokens.RefreshToken is null)
            {
                return tokens.AccessToken;
            }

            var refreshed = await idp.RefreshAsync(tokens.RefreshToken, cancellationToken);
            tokens = new UpstreamTokens(
                refreshed.AccessToken ?? tokens.AccessToken,
                refreshed.RefreshToken ?? tokens.RefreshToken,
                refreshed.ExpiresIn is { } seconds ? time.GetUtcNow().AddSeconds(seconds) : null);

            await store.SetAsync(AuthStore.SessionKey(sessionId),
                session with { EncryptedUpstreamTokens = crypto.Encrypt(JsonSerializer.Serialize(tokens)) },
                TimeSpan.FromDays(proxy.RefreshTokenLifetimeDays), cancellationToken);

            return tokens.AccessToken;
        }
        finally
        {
            gate.Release();
        }
    }
}
