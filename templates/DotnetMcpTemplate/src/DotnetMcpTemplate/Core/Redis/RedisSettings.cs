namespace DotnetMcpTemplate.Core.Redis;

/// <summary>
/// Used by the default <see cref="IRedisConnectionFactory"/>. Registered only when Redis is used (AUTH_STORE=redis or
/// <c>services.AddAppRedis(...)</c>), so apps without Redis need no values.
/// </summary>
public sealed class RedisSettings
{
    /// <summary>
    /// A StackExchange.Redis connection string (<c>host:6379,password=...,ssl=true</c>; several endpoints for a cluster;
    /// <c>serviceName=...</c> for Sentinel) or a URL (<c>redis://user:password@host:6379/0</c>, <c>rediss://</c> for TLS).
    /// A custom <see cref="IRedisConnectionFactory"/> may ignore it.
    /// </summary>
    [ConfigurationKeyName("REDIS_URL")]
    public string? Url { get; init; }
}
