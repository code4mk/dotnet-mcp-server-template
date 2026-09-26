using System.Text.Json;
using DotnetMcpTemplate.Core.Mcp;
using DotnetMcpTemplate.Core.Mcp.Filters;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotnetMcpTemplate.Core.Validation;

/// <summary>
/// Validates arguments before a tool or prompt runs. Tools get a result with <c>isError: true</c> listing every
/// problem (the model can fix its input and retry); prompts get an invalid-params error. The handler never runs.
/// </summary>
public static class ValidationFilter
{
    public static McpRequestHandler<CallToolRequestParams, CallToolResult> Tools(McpRequestHandler<CallToolRequestParams, CallToolResult> next) =>
        (context, cancellationToken) =>
        {
            var errors = ValidateCall(context.Services, registry => registry.Tools, context.Params?.Name, context.Params?.Arguments);
            return errors.Count == 0
                ? next(context, cancellationToken)
                : ValueTask.FromResult(ErrorFilter.ToolError(RequestValidator.Format(context.Params!.Name, errors)));
        };

    public static McpRequestHandler<GetPromptRequestParams, GetPromptResult> Prompts(McpRequestHandler<GetPromptRequestParams, GetPromptResult> next) =>
        (context, cancellationToken) =>
        {
            var errors = ValidateCall(context.Services, registry => registry.Prompts, context.Params?.Name, context.Params?.Arguments);
            return errors.Count == 0
                ? next(context, cancellationToken)
                : throw new McpProtocolException(RequestValidator.Format(context.Params!.Name, errors), McpErrorCode.InvalidParams);
        };

    private static IReadOnlyList<ValidationError> ValidateCall(
        IServiceProvider? services,
        Func<McpHandlerRegistry, Dictionary<string, System.Reflection.MethodInfo>> select,
        string? name,
        IDictionary<string, JsonElement>? arguments)
    {
        if (services is null || name is null
            || services.GetService<McpHandlerRegistry>() is not { } registry
            || !select(registry).TryGetValue(name, out var method))
        {
            return [];   // unknown names are reported by the SDK
        }

        var isService = services.GetService<IServiceProviderIsService>();
        return RequestValidator.Validate(method, arguments, type => IsInjected(type, isService), McpJsonUtilities.DefaultOptions);
    }

    private static bool IsInjected(Type type, IServiceProviderIsService? isService) =>
        type == typeof(CancellationToken)
        || type == typeof(System.Security.Claims.ClaimsPrincipal)
        || type == typeof(McpServer)
        || type == typeof(IServiceProvider)
        || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RequestContext<>))
        || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IProgress<>))
        || isService?.IsService(type) == true;
}
