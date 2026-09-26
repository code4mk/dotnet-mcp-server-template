using System.Text;

namespace DotnetMcpTemplate.Core.ApiClients.Handlers;

/// <summary>Adds a fixed header (bearer token, API key, basic credentials) to every request.</summary>
public sealed class StaticHeaderHandler(string name, string value) : DelegatingHandler
{
    public static StaticHeaderHandler Basic(string username, string password) =>
        new("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Remove(name);
        request.Headers.TryAddWithoutValidation(name, value);
        return base.SendAsync(request, cancellationToken);
    }
}
