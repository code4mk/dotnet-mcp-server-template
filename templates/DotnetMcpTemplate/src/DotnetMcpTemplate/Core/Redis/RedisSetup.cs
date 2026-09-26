using DotnetMcpTemplate.Core.Common.Settings;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace DotnetMcpTemplate.Core.Redis;

public static class RedisSetup
{
    /// <summary>
    /// Registers the shared Redis connection: <see cref="RedisConnection"/>, <see cref="IConnectionMultiplexer"/> and a
    /// "redis" check in GET /health. AUTH_STORE=redis calls it; call it yourself to use Redis in your own code.
    /// Calling it more than once is harmless.
    /// </summary>
    public static IServiceCollection AddAppRedis(this IServiceCollection services, IConfiguration configuration)
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(RedisConnection)))
        {
            return services;
        }

        services.AddEnvSettings<RedisSettings>(configuration);
        services.TryAddSingleton<IRedisConnectionFactory, DefaultRedisConnectionFactory>();
        services.AddSingleton<RedisConnection>();
        // Blocks only on first resolution; prefer RedisConnection.GetAsync() in async code.
        services.TryAddSingleton<IConnectionMultiplexer>(sp => sp.GetRequiredService<RedisConnection>().GetAsync().GetAwaiter().GetResult());
        services.AddHealthChecks().AddCheck<RedisHealthCheck>("redis");
        return services;
    }

    /// <summary>
    /// Uses your factory instead of the REDIS_URL default, e.g. for Azure Entra ID, AWS IAM or custom certificates.
    /// Works before or after <see cref="AddAppRedis"/>.
    /// </summary>
    public static IServiceCollection AddRedisConnectionFactory<TFactory>(this IServiceCollection services)
        where TFactory : class, IRedisConnectionFactory
    {
        services.RemoveAll<IRedisConnectionFactory>();
        services.AddSingleton<IRedisConnectionFactory, TFactory>();
        return services;
    }
}
