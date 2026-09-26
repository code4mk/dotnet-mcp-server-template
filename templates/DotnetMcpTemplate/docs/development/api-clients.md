# API clients

Each external API (data source) is a typed client in `Integrations/<Name>/` deriving from `ApiClient` (in
`Core/ApiClients/`) plus a set of `{PREFIX}_*` variables.

```csharp
public interface ICrmClient { Task<Customer?> GetCustomerAsync(string id, CancellationToken ct); }

public sealed class CrmClient(HttpClient http) : ApiClient(http), ICrmClient
{
    protected override string SourceName => "CRM";

    public Task<Customer?> GetCustomerAsync(string id, CancellationToken ct) =>
        GetAsync<Customer>($"customers/{Uri.EscapeDataString(id)}", cancellationToken: ct);
}

// Integrations/IntegrationsSetup.cs
services.AddApiClient<ICrmClient, CrmClient>(configuration, "CRM");
```

Layout: one folder per API (`Integrations/Crm/CrmClient.cs`, `Integrations/Crm/CrmModels.cs`), registered in
`Integrations/IntegrationsSetup.cs`. See `Integrations/SampleApi/` and the `SAMPLE_API_*` variables in `.env.example`.
Only services use clients; they map the API models to DTOs in `Models/` (enforced by the architecture tests).

`ApiClient` offers `GetAsync<T>`, `PostAsync<T>`, `PutAsync<T>`, `PatchAsync<T>`, `DeleteAsync`, per-request
`RequestHeaders`, query parameters, and `SendAsync(HttpRequestMessage)` for anything else. Async only; default
headers are set at registration (`{PREFIX}_HEADERS`), because the HttpClient is shared.

| Variable | Default | |
| --- | --- | --- |
| `{PREFIX}_BASE_URL` | required | Base URL (path segments kept) |
| `{PREFIX}_TIMEOUT_SECONDS` | 30 | Per attempt |
| `{PREFIX}_RETRY_COUNT` | 3 | Transient failures (5xx, 408, 429, network) |
| `{PREFIX}_RETRY_UNSAFE_METHODS` | false | Retry POST/PUT/PATCH/DELETE too |
| `{PREFIX}_HEADERS` | | `Name=Value;Other=Value` |
| `{PREFIX}_AUTH` | none | `none`, `bearer` (`_TOKEN`), `api_key` (`_API_KEY`, `_API_KEY_HEADER`), `basic` (`_USERNAME`, `_PASSWORD`), `client_credentials` (`_TOKEN_URL`, `_CLIENT_ID`, `_CLIENT_SECRET`, `_SCOPE`), `user` |

`user` calls the API with the signed-in user's IdP access token (oidc provider), refreshed automatically. It is not
the MCP client's token: forwarding that is forbidden by the MCP spec.

Every client gets retries with backoff, a circuit breaker, timeouts (Microsoft.Extensions.Http.Resilience) and the
`X-Correlation-Id` of the current request. Failures become `ApiClientException` with a safe message for the model;
details are logged only.
