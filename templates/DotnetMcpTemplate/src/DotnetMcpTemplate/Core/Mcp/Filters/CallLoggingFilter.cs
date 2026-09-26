using System.Diagnostics;
using System.Security.Claims;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotnetMcpTemplate.Core.Mcp.Filters;

/// <summary>Logs every tool call, prompt and resource read: name, user, duration and outcome. Never logs arguments.</summary>
public static class CallLoggingFilter
{
    public static McpRequestHandler<CallToolRequestParams, CallToolResult> Tools(McpRequestHandler<CallToolRequestParams, CallToolResult> next) =>
        async (context, cancellationToken) =>
        {
            var logger = GetLogger(context.Services);
            var started = Stopwatch.GetTimestamp();
            try
            {
                var result = await next(context, cancellationToken);
                logger?.LogInformation("Tool {Tool} by {User} finished in {ElapsedMs} ms (error: {IsError})",
                    context.Params?.Name, UserId(context.User), Elapsed(started), result.IsError == true);
                return result;
            }
            catch (Exception exception)
            {
                logger?.LogWarning("Tool {Tool} by {User} failed in {ElapsedMs} ms: {Error}",
                    context.Params?.Name, UserId(context.User), Elapsed(started), exception.Message);
                throw;
            }
        };

    public static McpRequestHandler<GetPromptRequestParams, GetPromptResult> Prompts(McpRequestHandler<GetPromptRequestParams, GetPromptResult> next) =>
        async (context, cancellationToken) =>
        {
            var logger = GetLogger(context.Services);
            var started = Stopwatch.GetTimestamp();
            try
            {
                var result = await next(context, cancellationToken);
                logger?.LogInformation("Prompt {Prompt} by {User} finished in {ElapsedMs} ms",
                    context.Params?.Name, UserId(context.User), Elapsed(started));
                return result;
            }
            catch (Exception exception)
            {
                logger?.LogWarning("Prompt {Prompt} by {User} failed in {ElapsedMs} ms: {Error}",
                    context.Params?.Name, UserId(context.User), Elapsed(started), exception.Message);
                throw;
            }
        };

    public static McpRequestHandler<ReadResourceRequestParams, ReadResourceResult> Resources(McpRequestHandler<ReadResourceRequestParams, ReadResourceResult> next) =>
        async (context, cancellationToken) =>
        {
            var logger = GetLogger(context.Services);
            var started = Stopwatch.GetTimestamp();
            try
            {
                var result = await next(context, cancellationToken);
                logger?.LogInformation("Resource {Uri} read by {User} in {ElapsedMs} ms",
                    context.Params?.Uri, UserId(context.User), Elapsed(started));
                return result;
            }
            catch (Exception exception)
            {
                logger?.LogWarning("Resource {Uri} read by {User} failed in {ElapsedMs} ms: {Error}",
                    context.Params?.Uri, UserId(context.User), Elapsed(started), exception.Message);
                throw;
            }
        };

    private static ILogger? GetLogger(IServiceProvider? services) =>
        services?.GetService<ILoggerFactory>()?.CreateLogger("DotnetMcpTemplate.Core.Mcp.Calls");

    private static string UserId(ClaimsPrincipal? user) =>
        user?.FindFirst("sub")?.Value ?? (user?.Identity?.IsAuthenticated == true ? user.Identity.Name ?? "user" : "anonymous");

    private static long Elapsed(long started) => (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
}
