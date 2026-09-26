using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace DotnetMcpTemplate.Capabilities.Prompts;

[McpServerPromptType]
[AllowAnonymous]
public sealed class SearchPrompts
{
    [McpServerPrompt(Name = "search_help", Title = "Help me search")]
    [Description("Guides the assistant to search projects well and read the most relevant results.")]
    public static ChatMessage SearchHelp(
        [StringLength(100), Description("What you're looking for (optional).")] string? topic = null) =>
        new(ChatRole.User, $"""
            Help me find projects{(string.IsNullOrWhiteSpace(topic) ? string.Empty : $" about \"{topic}\"")}.
            Use the `search` tool with 2-3 different short queries, then read the most relevant `projects://` resources
            and give me a short list: title, one-line summary and why it matches.
            """);
}
