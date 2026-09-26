using System.ComponentModel.DataAnnotations;
using DotnetMcpTemplate.Core.Common.Settings;

namespace DotnetMcpTemplate.Core.Mcp;

public sealed class McpSettings : IEnvSettings
{
    [ConfigurationKeyName("MCP_SERVER_NAME")]
    [Required, StringLength(100)]
    public string ServerName { get; init; } = "DotnetMcpTemplate";

    /// <summary>Path of the MCP endpoint. Clients connect to APP_URL + MCP_PATH.</summary>
    [ConfigurationKeyName("MCP_PATH")]
    [Required, RegularExpression("^/[A-Za-z0-9/_-]*$", ErrorMessage = "MCP_PATH must start with / and contain only letters, digits, /, _ and -.")]
    public string Path { get; init; } = "/mcp";

    /// <summary>Optional instructions sent to clients (what this server is for, how to use it).</summary>
    [ConfigurationKeyName("MCP_INSTRUCTIONS")]
    [StringLength(4000)]
    public string? Instructions { get; init; }

    /// <summary>MCP path without a trailing slash (except "/").</summary>
    public string NormalizedPath => Path.Length > 1 ? Path.TrimEnd('/') : Path;
}

public static class AppVersion
{
    public static string Current { get; } =
        typeof(AppVersion).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
}
