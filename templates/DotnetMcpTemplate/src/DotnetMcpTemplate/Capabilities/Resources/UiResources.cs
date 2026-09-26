using System.ComponentModel;
using DotnetMcpTemplate.Core.Mcp.Apps;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using McpAppsSdk = ModelContextProtocol.Extensions.Apps.McpApps;

namespace DotnetMcpTemplate.Capabilities.Resources;

/// <summary>
/// MCP App bundles from ui_dist/, one resource per entry. Link a tool to one with
/// <c>[McpAppUi(ResourceUri = "ui://entry")]</c>. The HTML itself holds no data, so it's public.
/// </summary>
[McpServerResourceType]
[AllowAnonymous]
public sealed class UiResources(UiBundles bundles)
{
    [McpServerResource(UriTemplate = "ui://projects-dashboard", Name = "projects_dashboard_ui", Title = "Projects dashboard", MimeType = McpAppsSdk.HtmlMimeType)]
    [McpMeta("ui", """{"prefersBorder":true}""")]
    [Description("Interactive projects dashboard (MCP App).")]
    public string ProjectsDashboard() => bundles.Read("projects-dashboard");
}
