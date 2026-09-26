using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DotnetMcpTemplate.Core.Redis;

/// <summary>Part of GET /health when Redis is used: PINGs the shared connection.</summary>
public sealed class RedisHealthCheck(RedisConnection redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = await (await redis.GetDatabaseAsync()).PingAsync();
            return HealthCheckResult.Healthy($"PING {latency.TotalMilliseconds:0.#} ms");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Redis is unreachable.", exception);
        }
    }
}
