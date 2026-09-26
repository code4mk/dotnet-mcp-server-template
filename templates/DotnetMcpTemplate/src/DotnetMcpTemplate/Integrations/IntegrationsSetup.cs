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

        return services;
    }
}
