# Testing

```bash
dotnet test                                   # everything
dotnet test tests/DotnetMcpTemplate.UnitTests # one project
dotnet test --filter "FullyQualifiedName~ToolTests"
cd ui && pnpm run typecheck                   # MCP App views
```

Tests mirror the source tree (`tests/*/Capabilities/...`, `tests/*/Core/...`). CI runs all of them plus the UI build.

| Project | Tests | Speed |
| --- | --- | --- |
| `UnitTests` | Services, validation, auth helpers, API client plumbing. No server | Milliseconds |
| `IntegrationTests` | The real server in memory, called through a real MCP client: tools, resources, prompts, auth, the full OAuth flow | ~1 s |
| `ArchitectureTests` | Rules that keep the code consistent (below) | Milliseconds |

## Unit tests: services

Services take their API clients by interface, so pass a fake. `tests/*.UnitTests/Fakes/FakeSampleApiClient.cs` is
an in-memory `ISampleApiClient`:

```csharp
private readonly FakeSampleApiClient _api = new();
private readonly ProjectService _service;

public ProjectServiceTests() => _service = new ProjectService(_api, NullLogger<ProjectService>.Instance);
```

Test business rules here (duplicates, limits, `AppException` kinds), not MCP shapes.

## Integration tests: the server as a client sees it

`McpServerFactory` starts the app with `WebApplicationFactory` and settings as environment variables (like
production), swaps external APIs for fakes, and connects a real `McpClient`:

```csharp
[Collection(ServerCollection.Name)]
public sealed class InvoiceToolTests(McpServerFactory factory) : IClassFixture<McpServerFactory>
{
    [Fact]
    public async Task Get_invoice_needs_the_invoices_scope()
    {
        await using var client = await factory.ConnectAsync(TestAuthProvider.Token("alice", scopes: "mcp:tools"));
        var result = await client.CallToolAsync("get_invoice", new Dictionary<string, object?> { ["number"] = "INV-000123" });

        Assert.True(result.IsError);
    }
}
```

- **Auth:** `AUTH_PROVIDER=test` accepts `Bearer test|{userId}|{scopes}`. Build tokens with
  `TestAuthProvider.Token("alice", scopes: "mcp:tools projects:write")`; `null` connects anonymously.
- **Fakes:** override `ConfigureTestServices` in a factory subclass to replace more services.
- **Settings:** pass overrides to the protected `McpServerFactory(overrides)` constructor (see
  `OidcProxyServerFactory`, which runs the whole OAuth flow against `FakeIdentityProvider`).
- **Every class joins `[Collection(ServerCollection.Name)]`:** settings are environment variables read at startup,
  so servers must not start in parallel.

## Architecture tests

`McpConventionTests` fail the build when:

- a tool, resource or prompt class has neither `[Authorize]` nor `[AllowAnonymous]`;
- a tool or prompt has no explicit, unique `Name`, or a resource no `UriTemplate` and `Name`;
- a tool, resource or prompt uses an API client (`Core/ApiClients`, `Integrations/`) instead of a service;
- a service depends on MCP types, or a service interface exposes API client models instead of DTOs.

Add a rule here when a code review comment keeps repeating.

## MCP App views

`pnpm run build` typechecks and builds; CI also fails when the committed `ui_dist/` bundles are stale. Each view
validates tool outputs against its `schema.ts` at runtime, so a C# record change that breaks a view shows up as a
clear message in the view. Preview every state with `pnpm dev` (sample data from `sandbox.ts`).
