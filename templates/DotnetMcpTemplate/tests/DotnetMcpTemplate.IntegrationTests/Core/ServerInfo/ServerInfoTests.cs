using System.Net;
using System.Net.Http.Json;
using DotnetMcpTemplate.IntegrationTests.TestInfrastructure;
using DotnetMcpTemplate.Core.ServerInfo;

namespace DotnetMcpTemplate.IntegrationTests.Core.ServerInfo;

[Collection(ServerCollection.Name)]
public sealed class ServerInfoTests(McpServerFactory factory) : IClassFixture<McpServerFactory>
{
    [Fact]
    public async Task Root_returns_minimal_json()
    {
        var info = await factory.CreateClient().GetFromJsonAsync<ServerInfoResponse>("/");

        Assert.Equal("ok", info!.Status);
        Assert.Equal("http://localhost/mcp", info.Mcp);
    }

    [Fact]
    public async Task Health_is_public()
    {
        var response = await factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Mcp_without_token_is_challenged_with_resource_metadata()
    {
        var response = await factory.CreateClient().PostAsync("/mcp", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("resource_metadata", response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task Protected_resource_metadata_names_the_mcp_url()
    {
        var json = await factory.CreateClient().GetStringAsync("/.well-known/oauth-protected-resource/mcp");
        Assert.Contains("http://localhost/mcp", json);
    }
}
