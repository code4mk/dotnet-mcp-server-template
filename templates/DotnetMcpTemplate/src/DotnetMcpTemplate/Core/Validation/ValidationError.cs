namespace DotnetMcpTemplate.Core.Validation;

/// <summary>One invalid argument. <paramref name="Path"/> uses JSON names, e.g. <c>request.durationDays</c>.</summary>
public sealed record ValidationError(string Path, string Message);
