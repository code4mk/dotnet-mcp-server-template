using DotnetMcpTemplate.Core.Auth;
using DotnetMcpTemplate.Core.Mcp.Filters;
using DotnetMcpTemplate.Core.Mcp.Apps;
using DotnetMcpTemplate.Core.Validation;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Extensions.Apps;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotnetMcpTemplate.Core.Mcp;

public static class McpSetup
{
    /// <summary>
    /// Registers the MCP server: every [McpServerToolType], [McpServerResourceType] and [McpServerPromptType]
    /// class in this project, authorization, validation, error mapping, call logging and MCP Apps.
    /// </summary>
    public static IServiceCollection AddAppMcpServer(this IServiceCollection services)
    {
        services.AddSingleton<McpHandlerRegistry>();
        services.AddSingleton<UiBundles>();

        services.AddOptions<McpServerOptions>()
            .Configure<McpSettings>((options, settings) =>
            {
                options.ServerInfo = new Implementation { Name = settings.ServerName, Version = AppVersion.Current };
                if (!string.IsNullOrWhiteSpace(settings.Instructions))
                {
                    options.ServerInstructions = settings.Instructions;
                }
            });

        services.AddMcpServer()
            .WithHttpTransport(options => options.Stateless = true)   // no sessions: scales horizontally
            .AddAuthorizationFilters()                                  // [Authorize] / [AllowAnonymous] on all three
            .WithToolsFromAssembly()
            .WithResourcesFromAssembly()
            .WithPromptsFromAssembly()
            .WithMcpApps()                                              // [McpAppUi] → _meta.ui.resourceUri
            .WithRequestFilters(filters =>
            {
                // Order: first registered runs outermost.
                filters.AddCallToolFilter(CallLoggingFilter.Tools);
                filters.AddCallToolFilter(ErrorFilter.Tools);
                filters.AddCallToolFilter(ValidationFilter.Tools);

                filters.AddGetPromptFilter(CallLoggingFilter.Prompts);
                filters.AddGetPromptFilter(ErrorFilter.Prompts);
                filters.AddGetPromptFilter(ValidationFilter.Prompts);

                filters.AddReadResourceFilter(CallLoggingFilter.Resources);
                filters.AddReadResourceFilter(ErrorFilter.Resources);
            });

        return services;
    }

    /// <summary>Maps the MCP endpoint at MCP_PATH. With MCP_AUTH_MODE=required the whole endpoint needs a token.</summary>
    public static WebApplication MapAppMcp(this WebApplication app)
    {
        var mcp = app.Services.GetRequiredService<McpSettings>();
        var auth = app.Services.GetRequiredService<AuthSettings>();

        var endpoint = app.MapMcp(mcp.NormalizedPath);
        if (auth.Mode == AuthModes.Required)
        {
            endpoint.RequireAuthorization();
        }

        // Fail fast on duplicate or unnamed tools/prompts (the validation filter needs explicit names).
        app.Services.GetRequiredService<McpHandlerRegistry>();

        return app;
    }
}
