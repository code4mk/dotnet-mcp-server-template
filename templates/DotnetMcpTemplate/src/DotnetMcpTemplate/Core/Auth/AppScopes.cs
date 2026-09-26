namespace DotnetMcpTemplate.Core.Auth;

/// <summary>
/// Scopes this MCP server understands. They are advertised in the OAuth metadata, and a client gets only the ones it
/// asks for. Add a scope here when you add a scope policy (see <see cref="Policies"/>).
/// </summary>
public static class AppScopes
{
    /// <summary>Use the MCP tools.</summary>
    public const string McpTools = "mcp:tools";

    /// <summary>Create and change projects.</summary>
    public const string ProjectsWrite = "projects:write";

    public static IReadOnlyList<string> All { get; } = [McpTools, ProjectsWrite];
}
