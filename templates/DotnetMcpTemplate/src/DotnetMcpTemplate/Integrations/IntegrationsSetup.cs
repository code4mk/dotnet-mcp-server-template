using DotnetMcpTemplate.Core.ApiClients;
using DotnetMcpTemplate.Integrations.SampleApi;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotnetMcpTemplate.Integrations;

public static class IntegrationsSetup
{
    /// <summary>One line per data source. Each reads its own {PREFIX}_* settings.</summary>
    public static IServiceCollection AddIntegrations(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();

        services.AddApiClient<ISampleApiClient, SampleApiClient>(configuration, SampleApiClient.Prefix);
        // services.AddApiClient<ICrmClient, CrmClient>(configuration, "CRM");

        // Redis (docs/development/redis.md). AUTH_STORE=redis registers it already; add it for your own caching, locks...
        // services.AddAppRedis(configuration);
        // Your vendor's connection (Azure Entra ID, AWS, GCP, cluster, certificates) instead of the REDIS_URL default:
        // services.AddRedisConnectionFactory<MyRedisConnectionFactory>();

        return services;
    }
}
