using System.ComponentModel;
using DotnetMcpTemplate.Services;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotnetMcpTemplate.Capabilities.Resources;

[McpServerResourceType]
[Authorize]
public sealed class ProjectResources(IProjectService projects)
{
    [McpServerResource(UriTemplate = "projects://{id}", Name = "project", Title = "Project", MimeType = "application/json")]
    [Description("A project's details by id.")]
    public async Task<TextResourceContents> GetProject(string id, CancellationToken cancellationToken) =>
        JsonResource.Create($"projects://{id}", await projects.GetAsync(ResourceArguments.PositiveInt(id, "id"), cancellationToken));
}
