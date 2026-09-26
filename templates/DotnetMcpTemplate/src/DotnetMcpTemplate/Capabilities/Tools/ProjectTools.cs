using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DotnetMcpTemplate.Core.Auth;
using DotnetMcpTemplate.Models.Requests;
using DotnetMcpTemplate.Models.Responses;
using DotnetMcpTemplate.Services;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Extensions.Apps;
using ModelContextProtocol.Server;

namespace DotnetMcpTemplate.Capabilities.Tools;

[McpServerToolType]
[Authorize]
public sealed class ProjectTools(IProjectService projects)
{
    [McpServerTool(Name = "list_projects", Title = "List projects", ReadOnly = true, Destructive = false, Idempotent = true)]
    [Description("Lists projects, optionally for one owner, one page at a time.")]
    public Task<PagedResult<ProjectDto>> ListProjects(
        [Range(1, int.MaxValue), Description("Only projects of this owner (user id).")] int? ownerUserId = null,
        [Range(1, 1000), Description("Page number, starting at 1.")] int page = 1,
        [Range(1, 50), Description("Results per page (1-50).")] int pageSize = 10,
        CancellationToken cancellationToken = default) =>
        projects.ListAsync(ownerUserId, page, pageSize, cancellationToken);

    /// <summary>Needs the projects:write scope on top of being signed in.</summary>
    [McpServerTool(Name = "create_project", Title = "Create project", Destructive = false, Idempotent = false)]
    [Authorize(Policy = Policies.ProjectsWrite)]
    [Description("Creates a project. High-priority projects must be 90 days or less.")]
    public Task<ProjectDto> CreateProject(CreateProjectRequest request, AppUser user, CancellationToken cancellationToken) =>
        projects.CreateAsync(request, user, cancellationToken);

    /// <summary>MCP App: clients that support MCP Apps render ui://projects-dashboard with this tool's result.</summary>
    [McpServerTool(Name = "show_projects_dashboard", Title = "Projects dashboard", ReadOnly = true, Destructive = false, UseStructuredContent = true)]
    [McpAppUi(ResourceUri = "ui://projects-dashboard")]
    [Description("Shows an interactive dashboard of projects per owner. Also returns the numbers as data.")]
    public Task<ProjectsSummary> ShowProjectsDashboard(CancellationToken cancellationToken) =>
        projects.GetSummaryAsync(cancellationToken);
}
