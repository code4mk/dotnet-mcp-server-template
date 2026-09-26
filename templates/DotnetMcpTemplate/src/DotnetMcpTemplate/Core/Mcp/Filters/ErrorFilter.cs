using DotnetMcpTemplate.Core.ApiClients;
using DotnetMcpTemplate.Core.Common.Errors;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotnetMcpTemplate.Core.Mcp.Filters;

/// <summary>
/// Turns exceptions into clear MCP errors. Tools return a result with <c>isError: true</c> (the model reads it and
/// can retry); resources and prompts return an MCP error. Internal details never reach the client.
/// </summary>
public static class ErrorFilter
{
    public static McpRequestHandler<CallToolRequestParams, CallToolResult> Tools(McpRequestHandler<CallToolRequestParams, CallToolResult> next) =>
        async (context, cancellationToken) =>
        {
            try
            {
                return await next(context, cancellationToken);
            }
            catch (Exception exception) when (ShouldMap(exception, cancellationToken))
            {
                return ToolError(Describe(exception, context.Services, context.Params?.Name));
            }
        };

    public static McpRequestHandler<GetPromptRequestParams, GetPromptResult> Prompts(McpRequestHandler<GetPromptRequestParams, GetPromptResult> next) =>
        async (context, cancellationToken) =>
        {
            try
            {
                return await next(context, cancellationToken);
            }
            catch (Exception exception) when (ShouldMap(exception, cancellationToken))
            {
                throw ToProtocolError(exception, context.Services, context.Params?.Name);
            }
        };

    public static McpRequestHandler<ReadResourceRequestParams, ReadResourceResult> Resources(McpRequestHandler<ReadResourceRequestParams, ReadResourceResult> next) =>
        async (context, cancellationToken) =>
        {
            try
            {
                return await next(context, cancellationToken);
            }
            catch (Exception exception) when (ShouldMap(exception, cancellationToken))
            {
                throw ToProtocolError(exception, context.Services, context.Params?.Uri);
            }
        };

    public static CallToolResult ToolError(string message) => new()
    {
        IsError = true,
        Content = [new TextContentBlock { Text = message }],
    };

    // MCP protocol errors (auth, validation, unknown tool) are already shaped by the SDK or our filters.
    private static bool ShouldMap(Exception exception, CancellationToken cancellationToken) =>
        exception is not McpProtocolException
        && !(exception is OperationCanceledException && cancellationToken.IsCancellationRequested);

    private static McpProtocolException ToProtocolError(Exception exception, IServiceProvider? services, string? target)
    {
        var message = Describe(exception, services, target);
        var code = exception is AppException { Kind: AppErrorKind.NotFound or AppErrorKind.BusinessRule }
            ? McpErrorCode.InvalidParams
            : McpErrorCode.InternalError;
        return new McpProtocolException(message, code);
    }

    private static string Describe(Exception exception, IServiceProvider? services, string? target)
    {
        switch (exception)
        {
            case AppException app:
                return app.Message;
            case ApiClientException api:
                return api.SafeMessage;
            case McpException mcp:
                return mcp.Message;   // intentionally user-facing
            default:
                var logger = services?.GetService<ILoggerFactory>()?.CreateLogger(typeof(ErrorFilter));
                var correlationId = services?.GetService<IHttpContextAccessor>()?.HttpContext?.TraceIdentifier;
                logger?.LogError(exception, "Unhandled error in MCP handler {Target}", target);
                return $"An unexpected error occurred. Correlation id: {correlationId ?? "n/a"}.";
        }
    }
}
