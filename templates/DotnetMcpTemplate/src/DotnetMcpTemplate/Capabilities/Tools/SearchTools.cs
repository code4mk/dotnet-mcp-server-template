using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DotnetMcpTemplate.Models.Responses;
using DotnetMcpTemplate.Services;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace DotnetMcpTemplate.Capabilities.Tools;

/// <summary>Public tools: callable without signing in when MCP_AUTH_MODE=mixed.</summary>
[McpServerToolType]
[AllowAnonymous]
public sealed class SearchTools(ISearchService search)
{
    [McpServerTool(Name = "search", Title = "Search projects", ReadOnly = true, Destructive = false, Idempotent = true)]
    [Description("Searches public projects by text. Each result has a resource URI you can read for details.")]
    public Task<IReadOnlyList<SearchResultDto>> Search(
        [Required, StringLength(100, MinimumLength = 2), Description("Words to search for.")] string query,
        [Range(1, 20), Description("Maximum number of results (1-20).")] int limit = 5,
        CancellationToken cancellationToken = default) =>
        search.SearchAsync(query, limit, cancellationToken);
}
