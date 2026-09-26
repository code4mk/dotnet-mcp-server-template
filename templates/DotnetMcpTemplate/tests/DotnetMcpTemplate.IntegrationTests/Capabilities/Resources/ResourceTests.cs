using DotnetMcpTemplate.IntegrationTests.TestInfrastructure;
using ModelContextProtocol;
using McpAppsSdk = ModelContextProtocol.Extensions.Apps.McpApps;
using ModelContextProtocol.Protocol;

namespace DotnetMcpTemplate.IntegrationTests.Capabilities.Resources;

[Collection(ServerCollection.Name)]
public sealed class ResourceTests(McpServerFactory factory) : IClassFixture<McpServerFactory>
{
    [Fact]
    public async Task Reads_a_templated_resource()
    {
        await using var client = await factory.ConnectAsync(TestAuthProvider.Token("alice"));
        var result = await client.ReadResourceAsync("users://1");

        var content = Assert.IsType<TextResourceContents>(Assert.Single(result.Contents));
        Assert.Equal("application/json", content.MimeType);
        Assert.Contains("Leanne", content.Text);
    }

    [Fact]
    public async Task Invalid_uri_values_are_rejected()
    {
        await using var client = await factory.ConnectAsync(TestAuthProvider.Token("alice"));
        await Assert.ThrowsAnyAsync<McpException>(async () => await client.ReadResourceAsync("users://abc"));
    }

    [Fact]
    public async Task Serves_the_mcp_app_bundle()
    {
        await using var client = await factory.ConnectAsync(TestAuthProvider.Token("alice"));
        var result = await client.ReadResourceAsync("ui://projects-dashboard");

        var content = Assert.IsType<TextResourceContents>(Assert.Single(result.Contents));
        Assert.Equal(McpAppsSdk.HtmlMimeType, content.MimeType);
        Assert.Contains("<html", content.Text);
    }
}
