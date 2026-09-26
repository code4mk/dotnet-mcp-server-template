using System.Net;

namespace DotnetMcpTemplate.Core.ApiClients;

/// <summary>
/// An external API call failed. <see cref="SafeMessage"/> is what tools return to the model;
/// the full detail (status, body excerpt) is only logged.
/// </summary>
public sealed class ApiClientException(
    string source,
    string safeMessage,
    HttpStatusCode? statusCode = null,
    string? detail = null,
    Exception? innerException = null)
    : Exception($"{source}: {safeMessage}{(detail is null ? string.Empty : " " + detail)}", innerException)
{
    /// <summary>The external API that failed (hides <see cref="Exception.Source"/>, which names the throwing assembly).</summary>
    public new string Source { get; } = source;

    public HttpStatusCode? StatusCode { get; } = statusCode;

    public string SafeMessage { get; } = safeMessage;

    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
}
