using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DotnetMcpTemplate.Models.Requests;
using DotnetMcpTemplate.Models.Responses;
using DotnetMcpTemplate.Services;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace DotnetMcpTemplate.Capabilities.Tools;

/// <summary>Tools are thin: declare the MCP shape, take validated input, call one service method.</summary>
[McpServerToolType]
[Authorize]
public sealed class UserTools(IUserService users)
{
    [McpServerTool(Name = "get_user", Title = "Get user", ReadOnly = true, Destructive = false, Idempotent = true)]
    [Description("Gets one user by id.")]
    public Task<UserDto> GetUser(
        [Range(1, int.MaxValue), Description("The user's id.")] int id,
        CancellationToken cancellationToken) =>
        users.GetAsync(id, cancellationToken);

    [McpServerTool(Name = "search_users", Title = "Search users", ReadOnly = true, Destructive = false, Idempotent = true)]
    [Description("Finds users by name, username or email, one page at a time.")]
    public Task<PagedResult<UserDto>> SearchUsers(SearchUsersRequest request, CancellationToken cancellationToken) =>
        users.SearchAsync(request, cancellationToken);
}
