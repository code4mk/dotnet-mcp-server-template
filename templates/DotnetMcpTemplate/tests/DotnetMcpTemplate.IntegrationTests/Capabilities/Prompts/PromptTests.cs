using DotnetMcpTemplate.IntegrationTests.TestInfrastructure;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace DotnetMcpTemplate.IntegrationTests.Capabilities.Prompts;

[Collection(ServerCollection.Name)]
public sealed class PromptTests(McpServerFactory factory) : IClassFixture<McpServerFactory>
{
    [Fact]
    public async Task Prompt_embeds_service_data()
    {
        await using var client = await factory.ConnectAsync(TestAuthProvider.Token("alice"));
        var result = await client.GetPromptAsync("summarize_user", new Dictionary<string, object?> { ["userId"] = 1 });

        var text = string.Concat(result.Messages.Select(m => m.Content).OfType<TextContentBlock>().Select(c => c.Text));
        Assert.Contains("Leanne", text);
    }

    [Fact]
    public async Task Prompt_arguments_are_validated()
    {
        await using var client = await factory.ConnectAsync(TestAuthProvider.Token("alice"));
        await Assert.ThrowsAnyAsync<McpException>(async () =>
            await client.GetPromptAsync("summarize_user", new Dictionary<string, object?> { ["userId"] = 50 }));
    }
}
