using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using DotnetMcpTemplate.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace DotnetMcpTemplate.Capabilities.Prompts;

/// <summary>Prompts can load data through services and embed it, so the conversation starts with context.</summary>
[McpServerPromptType]
[Authorize]
public sealed class UserPrompts(IUserService users)
{
    [McpServerPrompt(Name = "summarize_user", Title = "Summarize a user")]
    [Description("Summarizes a user's profile for a colleague.")]
    public async Task<IEnumerable<ChatMessage>> SummarizeUser(
        [Range(1, 10), Description("The user's id (1-10 in the sample API).")] int userId,
        CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(userId, cancellationToken);
        return
        [
            new(ChatRole.User, "Summarize this user for a new colleague in 3 short bullet points. Mention how to contact them."),
            new(ChatRole.User, JsonSerializer.Serialize(user)),
        ];
    }
}
