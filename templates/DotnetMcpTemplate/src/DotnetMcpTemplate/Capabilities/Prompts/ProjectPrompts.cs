using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using DotnetMcpTemplate.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace DotnetMcpTemplate.Capabilities.Prompts;

[McpServerPromptType]
[Authorize]
public sealed class ProjectPrompts(IProjectService projects)
{
    [McpServerPrompt(Name = "project_status_report", Title = "Project status report")]
    [Description("Drafts a status report for a project, written for the chosen audience.")]
    public async Task<IEnumerable<ChatMessage>> StatusReport(
        [Range(1, int.MaxValue), Description("The project id.")] int projectId,
        [AllowedValues("team", "executives"), Description("team (detailed) or executives (short).")] string audience = "team",
        CancellationToken cancellationToken = default)
    {
        var project = await projects.GetAsync(projectId, cancellationToken);
        var style = audience == "executives"
            ? "Keep it to 5 lines: status, key risk, next milestone, decision needed."
            : "Include progress, risks with owners, next steps and open questions.";

        return
        [
            new(ChatRole.User, $"Write a status report for this project. {style}"),
            new(ChatRole.User, JsonSerializer.Serialize(project)),
        ];
    }
}
