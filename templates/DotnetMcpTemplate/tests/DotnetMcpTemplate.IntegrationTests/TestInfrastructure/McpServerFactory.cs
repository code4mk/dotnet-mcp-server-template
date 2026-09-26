using System.Net.Http.Headers;
using DotnetMcpTemplate.Integrations.SampleApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModelContextProtocol.Client;

namespace DotnetMcpTemplate.IntegrationTests.TestInfrastructure;

/// <summary>Starts the real server in memory. Settings go through environment variables, like in production.</summary>
public class McpServerFactory : WebApplicationFactory<Program>
{
    public McpServerFactory() : this(new Dictionary<string, string?>())
    {
    }

    protected McpServerFactory(IDictionary<string, string?> overrides)
    {
        var settings = new Dictionary<string, string?>
        {
            ["APP_ENV"] = "dev",
            ["APP_URL"] = "http://localhost",
            ["MCP_PATH"] = "/mcp",
            ["MCP_AUTH_MODE"] = "required",
            ["AUTH_PROVIDER"] = "test",
            ["SAMPLE_API_BASE_URL"] = "http://sample-api.test/",
        };
        foreach (var (key, value) in overrides)
        {
            settings[key] = value;
        }

        foreach (var (key, value) in settings)
        {
            Environment.SetEnvironmentVariable(key, value);
        }

        _ = Server;   // start now, while these environment variables are set
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISampleApiClient>();
            services.AddSingleton<ISampleApiClient, FakeSampleApiClient>();
            ConfigureTestServices(services);
        });
    }

    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }

    /// <summary>An MCP client connected to /mcp. Pass a bearer token or null for anonymous.</summary>
    public async Task<McpClient> ConnectAsync(string? bearerToken)
    {
        var http = CreateClient();
        if (bearerToken is not null)
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(http.BaseAddress!, "/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp,
            },
            http,
            ownsHttpClient: true);

        return await McpClient.CreateAsync(transport);
    }
}
