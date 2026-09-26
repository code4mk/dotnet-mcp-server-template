using DotnetMcpTemplate.IntegrationTests.TestInfrastructure;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace DotnetMcpTemplate.IntegrationTests.Capabilities.Tools;

[Collection(ServerCollection.Name)]
public sealed class ToolTests(McpServerFactory factory) : IClassFixture<McpServerFactory>
{
    private static string Text(CallToolResult result) => string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text));

    [Fact]
    public async Task Lists_the_sample_tools()
    {
        await using var client = await factory.ConnectAsync(TestAuthProvider.Token("alice"));
        var names = (await client.ListToolsAsync()).Select(t => t.Name).ToList();

        Assert.Contains("search", names);
        Assert.Contains("whoami", names);
        Assert.Contains("show_projects_dashboard", names);
    }

    [Fact]
    public async Task Whoami_returns_the_signed_in_user()
    {
        await using var client = await factory.ConnectAsync(TestAuthProvider.Token("alice"));
        var result = await client.CallToolAsync("whoami");

        Assert.NotEqual(true, result.IsError);
        Assert.Contains("alice", Text(result));
    }

    [Fact]
    public async Task Invalid_arguments_return_a_fixable_tool_error()
    {
        await using var client = await factory.ConnectAsync(TestAuthProvider.Token("alice"));
        var result = await client.CallToolAsync("search", new Dictionary<string, object?> { ["query"] = "a", ["limit"] = 99 });

        Assert.True(result.IsError);
        Assert.Contains("query", Text(result));
        Assert.Contains("limit", Text(result));
    }

    [Fact]
    public async Task Business_errors_are_returned_to_the_model()
    {
        await using var client = await factory.ConnectAsync(TestAuthProvider.Token("alice"));
        var result = await client.CallToolAsync("get_user", new Dictionary<string, object?> { ["id"] = 999 });

        Assert.True(result.IsError);
        Assert.Contains("not found", Text(result));
    }

    [Fact]
    public async Task Create_project_needs_the_write_scope()
    {
        await using var client = await factory.ConnectAsync(TestAuthProvider.Token("alice", scopes: "mcp:tools"));
        var arguments = new Dictionary<string, object?>
        {
            ["request"] = new { name = "Scoped project", priority = "low", durationDays = 10, ownerUserId = 1 },
        };

        await Assert.ThrowsAnyAsync<McpException>(async () => await client.CallToolAsync("create_project", arguments));
    }

    [Fact]
    public async Task Create_project_works_with_the_write_scope()
    {
        await using var client = await factory.ConnectAsync(TestAuthProvider.Token("bob", scopes: "mcp:tools projects:write"));
        var result = await client.CallToolAsync("create_project", new Dictionary<string, object?>
        {
            ["request"] = new { name = "Brand new project", priority = "medium", durationDays = 30, ownerUserId = 2 },
        });

        Assert.NotEqual(true, result.IsError);
        Assert.Contains("Brand new project", Text(result));
    }

    [Fact]
    public async Task Tools_the_user_cannot_call_are_hidden()
    {
        await using var client = await factory.ConnectAsync(TestAuthProvider.Token("alice", scopes: "mcp:tools"));
        var names = (await client.ListToolsAsync()).Select(t => t.Name).ToList();

        Assert.DoesNotContain("create_project", names);
    }
}
