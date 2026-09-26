using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DotnetMcpTemplate.Core.ApiClients.Handlers;

/// <summary>{PREFIX}_AUTH=client_credentials: adds a service token and retries once on 401 with a fresh token.</summary>
public sealed class ClientCredentialsHandler(ClientCredentialsTokenProvider provider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await provider.GetTokenAsync(forceRefresh: false, cancellationToken));
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && request.Content is null)
        {
            response.Dispose();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await provider.GetTokenAsync(forceRefresh: true, cancellationToken));
            response = await base.SendAsync(request, cancellationToken);
        }

        return response;
    }
}

/// <summary>Gets and caches an OAuth client-credentials token for one data source (singleton per prefix).</summary>
public sealed class ClientCredentialsTokenProvider(ApiClientSettings settings, IHttpClientFactory httpClientFactory, TimeProvider time)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt;

    public async Task<string> GetTokenAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        if (!forceRefresh && _token is not null && time.GetUtcNow() < _expiresAt)
        {
            return _token;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && _token is not null && time.GetUtcNow() < _expiresAt)
            {
                return _token;
            }

            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = settings.ClientId!,
                ["client_secret"] = settings.ClientSecret!,
            };
            if (!string.IsNullOrWhiteSpace(settings.Scope))
            {
                form["scope"] = settings.Scope;
            }

            using var response = await httpClientFactory.CreateClient().PostAsync(settings.TokenUrl, new FormUrlEncodedContent(form), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new ApiClientException(settings.Prefix, $"Could not get a service token for {settings.Prefix}.", response.StatusCode);
            }

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
            _token = token?.AccessToken ?? throw new ApiClientException(settings.Prefix, $"The token endpoint for {settings.Prefix} returned no token.");
            _expiresAt = time.GetUtcNow().AddSeconds(Math.Max(30, (token.ExpiresIn ?? 300) - 60));
            return _token;
        }
        finally
        {
            _gate.Release();
        }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
    }
}
