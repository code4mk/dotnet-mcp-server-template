using System.ComponentModel;
using DotnetMcpTemplate.Core.Mcp;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace DotnetMcpTemplate.Capabilities.Resources;

/// <summary>A direct (non-template) public resource.</summary>
[McpServerResourceType]
[AllowAnonymous]
public sealed class ServerResources(McpSettings settings)
{
    [McpServerResource(UriTemplate = "info://server", Name = "server_info", Title = "About this server", MimeType = "text/markdown")]
    [Description("What this MCP server offers and how to use it.")]
    public string GetServerInfo() => $"""
        # {settings.ServerName} {AppVersion.Current}

        - Tools: `search` (public), `whoami`, `get_user`, `search_users`, `list_projects`, `create_project` (needs `projects:write`), `show_projects_dashboard`.
        - Resources: `users://{"{id}"}`, `projects://{"{id}"}`, `ui://projects-dashboard`.
        - Prompts: `search_help`, `summarize_user`, `project_status_report`.
        """;
}
