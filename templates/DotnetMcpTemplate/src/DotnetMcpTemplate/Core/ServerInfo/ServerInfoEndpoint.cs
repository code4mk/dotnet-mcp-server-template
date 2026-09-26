using DotnetMcpTemplate.Core.Common.Settings;
using DotnetMcpTemplate.Core.Mcp;

namespace DotnetMcpTemplate.Core.ServerInfo;

public static class ServerInfoEndpoint
{
    /// <summary>GET / → minimal public information: name, version, status and the MCP URL.</summary>
    public static IEndpointRouteBuilder MapServerInfo(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", (AppSettings settings, McpSettings mcp) => TypedResults.Ok(new ServerInfoResponse(
                Name: mcp.ServerName,
                Version: AppVersion.Current,
                Status: "ok",
                Mcp: settings.PublicUrl + mcp.NormalizedPath)))
            .AllowAnonymous()
            .ExcludeFromDescription();

        return app;
    }
}

public sealed record ServerInfoResponse(string Name, string Version, string Status, string Mcp);
