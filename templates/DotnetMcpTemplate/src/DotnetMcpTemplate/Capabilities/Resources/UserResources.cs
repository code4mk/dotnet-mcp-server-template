using System.ComponentModel;
using DotnetMcpTemplate.Services;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotnetMcpTemplate.Capabilities.Resources;

[McpServerResourceType]
[Authorize]
public sealed class UserResources(IUserService users)
{
    [McpServerResource(UriTemplate = "users://{id}", Name = "user", Title = "User", MimeType = "application/json")]
    [Description("A user's profile by id.")]
    public async Task<TextResourceContents> GetUser(string id, CancellationToken cancellationToken) =>
        JsonResource.Create($"users://{id}", await users.GetAsync(ResourceArguments.PositiveInt(id, "id"), cancellationToken));
}
