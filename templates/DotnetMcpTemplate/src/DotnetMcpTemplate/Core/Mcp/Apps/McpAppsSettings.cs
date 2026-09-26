using DotnetMcpTemplate.Core.Common.Settings;

namespace DotnetMcpTemplate.Core.Mcp.Apps;

public sealed class McpAppsSettings : IEnvSettings
{
    /// <summary>Directory holding built UI bundles (one .html per entry). Empty = ui_dist next to the app.</summary>
    [ConfigurationKeyName("MCP_UI_DIST_DIR")]
    public string? DistDir { get; init; }
}
