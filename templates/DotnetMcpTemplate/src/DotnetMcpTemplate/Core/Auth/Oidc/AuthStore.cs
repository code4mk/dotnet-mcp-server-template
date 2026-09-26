using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>
/// Short-lived OAuth state on top of IDistributedCache (memory or Redis): client registrations, pending logins,
/// authorization codes, sessions and refresh tokens. Codes and tokens are stored by hash.
/// </summary>
public sealed class AuthStore(IDistributedCache cache)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task SetAsync<T>(string key, T value, TimeSpan lifetime, CancellationToken cancellationToken) =>
        cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(value, Json),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = lifetime }, cancellationToken);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        var bytes = await cache.GetAsync(key, cancellationToken);
        return bytes is null ? null : JsonSerializer.Deserialize<T>(bytes, Json);
    }

    /// <summary>Reads and deletes (single use). Best effort: not atomic across instances.</summary>
    public async Task<T?> TakeAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        var value = await GetAsync<T>(key, cancellationToken);
        if (value is not null)
        {
            await cache.RemoveAsync(key, cancellationToken);
        }

        return value;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken) => cache.RemoveAsync(key, cancellationToken);

    public static string ClientKey(string clientId) => "client:" + clientId;

    public static string TransactionKey(string id) => "txn:" + id;

    public static string CodeKey(string code) => "code:" + AuthCrypto.Hash(code);

    public static string RefreshKey(string token) => "refresh:" + AuthCrypto.Hash(token);

    public static string RotatedKey(string token) => "rotated:" + AuthCrypto.Hash(token);

    public static string SessionKey(string sessionId) => "session:" + sessionId;
}
