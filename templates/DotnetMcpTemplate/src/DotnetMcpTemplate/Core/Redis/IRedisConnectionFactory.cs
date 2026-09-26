using StackExchange.Redis;

namespace DotnetMcpTemplate.Core.Redis;

/// <summary>
/// Creates the app's one Redis connection. The default reads REDIS_URL. Replace it when your Redis needs code to
/// connect (Azure Entra ID, AWS IAM, custom CA certificates, secrets from a vault, ...):
/// <c>services.AddRedisConnectionFactory&lt;MyRedisConnectionFactory&gt;()</c>. See docs/development/redis.md.
/// </summary>
public interface IRedisConnectionFactory
{
    /// <summary>Called once for the app's lifetime, and again only if a previous attempt threw.</summary>
    Task<IConnectionMultiplexer> ConnectAsync(CancellationToken cancellationToken);
}

/// <summary>Connects with REDIS_URL (see <see cref="RedisSettings.Url"/> for the formats).</summary>
public sealed class DefaultRedisConnectionFactory(RedisSettings settings) : IRedisConnectionFactory
{
    public async Task<IConnectionMultiplexer> ConnectAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.Url))
        {
            throw new InvalidOperationException(
                "REDIS_URL is empty. Set it, or register your own IRedisConnectionFactory (docs/development/redis.md).");
        }

        return await ConnectionMultiplexer.ConnectAsync(RedisConnectionString.Parse(settings.Url));
    }
}
