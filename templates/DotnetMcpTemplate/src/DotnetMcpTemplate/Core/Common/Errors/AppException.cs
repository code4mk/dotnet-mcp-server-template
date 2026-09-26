namespace DotnetMcpTemplate.Core.Common.Errors;

public enum AppErrorKind
{
    /// <summary>Input is fine but a business rule rejects it.</summary>
    BusinessRule,
    NotFound,
    Conflict,
    Forbidden,
}

/// <summary>
/// An expected business error thrown by services. The MCP error filter turns it into a clear message for the
/// model (tools: a result with <c>isError: true</c>; resources/prompts: an invalid-params error).
/// Messages must be safe to show to users and models.
/// </summary>
public class AppException(AppErrorKind kind, string code, string message) : Exception(message)
{
    public AppErrorKind Kind { get; } = kind;

    /// <summary>Stable machine-readable code, e.g. <c>projects.name_exists</c>.</summary>
    public string Code { get; } = code;

    public static AppException NotFound(string code, string message) => new(AppErrorKind.NotFound, code, message);

    public static AppException Conflict(string code, string message) => new(AppErrorKind.Conflict, code, message);

    public static AppException Forbidden(string code, string message) => new(AppErrorKind.Forbidden, code, message);

    public static AppException Rule(string code, string message) => new(AppErrorKind.BusinessRule, code, message);
}
