using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace DotnetMcpTemplate.Core.ApiClients;

/// <summary>Per-request headers, e.g. <c>new() { ["If-Match"] = etag }</c>.</summary>
public sealed class RequestHeaders : Dictionary<string, string>
{
    public RequestHeaders() : base(StringComparer.OrdinalIgnoreCase)
    {
    }
}

/// <summary>
/// Base class for typed API clients: one subclass per data source (see ApiClients/SampleApi).
/// Base URL, default headers, auth, retries, timeouts and correlation ids come from the registration
/// (<c>AddApiClient</c>), so subclasses only describe the endpoints.
/// </summary>
/// <remarks>
/// Async only (sync HTTP blocks server threads). Headers are per request or set at registration: the underlying
/// HttpClient is shared by concurrent requests, so its default headers must not change at runtime.
/// </remarks>
public abstract class ApiClient(HttpClient http)
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Name used in errors and logs. Defaults to the class name.</summary>
    protected virtual string SourceName => GetType().Name;

    protected HttpClient Http => http;

    protected Task<T?> GetAsync<T>(string path, IReadOnlyDictionary<string, string?>? query = null, RequestHeaders? headers = null, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Get, path, query, body: null, headers, cancellationToken);

    protected Task<T?> PostAsync<T>(string path, object? body = null, RequestHeaders? headers = null, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Post, path, query: null, body, headers, cancellationToken);

    protected Task<T?> PutAsync<T>(string path, object? body = null, RequestHeaders? headers = null, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Put, path, query: null, body, headers, cancellationToken);

    protected Task<T?> PatchAsync<T>(string path, object? body = null, RequestHeaders? headers = null, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Patch, path, query: null, body, headers, cancellationToken);

    protected async Task DeleteAsync(string path, IReadOnlyDictionary<string, string?>? query = null, RequestHeaders? headers = null, CancellationToken cancellationToken = default) =>
        await SendAsync<JsonElement?>(HttpMethod.Delete, path, query, body: null, headers, cancellationToken);

    /// <summary>Sends a request and deserializes the JSON response (default for empty bodies).</summary>
    protected async Task<T?> SendAsync<T>(
        HttpMethod method,
        string path,
        IReadOnlyDictionary<string, string?>? query,
        object? body,
        RequestHeaders? headers,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, BuildUri(path, query));
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: Json);
        }

        foreach (var (name, value) in headers ?? [])
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }

        using var response = await SendAsync(request, cancellationToken);
        if (response.Content.Headers.ContentLength == 0 || response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return default;
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new ApiClientException(SourceName, $"The {SourceName} service returned an unexpected response.",
                response.StatusCode, exception.Message, exception);
        }
    }

    /// <summary>Escape hatch for anything else (streams, files, custom content). Non-success responses throw.</summary>
    protected async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Network errors, timeouts and open circuit breakers (after retries).
            throw new ApiClientException(SourceName, $"The {SourceName} service is unavailable right now. Try again later.",
                detail: exception.Message, innerException: exception);
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        using (response)
        {
            var excerpt = await ReadExcerptAsync(response, cancellationToken);
            var status = (int)response.StatusCode;
            var message = status switch
            {
                404 => $"The {SourceName} service could not find the requested item.",
                401 or 403 => $"The {SourceName} service refused access.",
                429 => $"The {SourceName} service is rate limiting requests. Try again later.",
                >= 500 => $"The {SourceName} service failed. Try again later.",
                _ => $"The {SourceName} service rejected the request ({status}).",
            };
            throw new ApiClientException(SourceName, message, response.StatusCode, $"{request.Method} {request.RequestUri} → {status}: {excerpt}");
        }
    }

    private static string BuildUri(string path, IReadOnlyDictionary<string, string?>? query)
    {
        var relative = path.TrimStart('/');   // keep BaseAddress path segments
        return query is null or { Count: 0 }
            ? relative
            : QueryHelpers.AddQueryString(relative, query.Where(p => p.Value is not null));
    }

    private static async Task<string> ReadExcerptAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            return text.Length > 500 ? text[..500] + "…" : text;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
