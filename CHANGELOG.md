# Changelog

## [Unreleased]

### Changed

- Folder layout: tools, resources and prompts live in `Capabilities/`; external API clients in `Integrations/`;
  plumbing in `Core/` (`ApiClients` base, `Auth`, `Mcp` with `Filters/` and `Apps/`, `Validation`, `Common`,
  `ServerInfo`). Namespaces follow folders and tests mirror the source tree. `AddApiClients` is now `AddIntegrations`.
- `global.json` pins the .NET SDK (CI and Docker use it).
- Watch mode: `dotnet watch` + `pnpm run watch` documented in getting-started; in dev, MCP App bundles are read
  from the project's `ui_dist/`, so rebuilt bundles are served without a restart.
- Startup logs the public URL (`Server running at ...`) next to Kestrel's `http://[::]:port` bind address.
- Consent page redesigned after the FastMCP OAuth proxy screen: server and client names, the redirect URI in a
  "Credentials will be sent to" box, collapsible advanced details (client id, scopes), Allow Access / Deny, a
  "Why am I seeing this?" tooltip and dark mode. The error page uses the same style.
- Docs: index at `docs/README.md`, a scopes section and OIDC callback / discovery troubleshooting in the auth guide,
  and updated paths in every guide.
- Scopes are defined in code (`Core/Auth/AppScopes.cs`) instead of `AUTH_SCOPES_SUPPORTED`; the dev user always has
  every scope and the `admin` role (`AUTH_DEV_USER_ROLES` removed).

### Fixed

- A client that requests no scope now gets no extra permissions instead of every scope.
- The server listens on IPv4 and IPv6, so `localhost` works when it resolves to `::1`.
- `Microsoft.Extensions.Http.Resilience` pinned to 10.9.0 (10.8.3 does not exist, NU1603).
- CS0114 warning on `ApiClientException.Source`, and a unit test that did not compile.

## [1.0.0] - 2026-09-26

### Added

- MCP server on ASP.NET Core with ModelContextProtocol.AspNetCore 2.0 (stateless Streamable HTTP).
- Tools, resources (templated, direct, MCP App `ui://`) and prompts, grouped by component, on a service layer.
- Argument validation for tools and prompts (data annotations, nested objects, `IValidatableObject`).
- Auth: OIDC proxy for any identity provider (DCR, consent, PKCE, ID token + userinfo merge, rotating refresh
  tokens, encrypted IdP tokens), `jwt` resource-server provider, extensible `IAuthProvider`, dev mode.
- Typed API clients (`ApiClient` base) with env-configured auth, retries, circuit breaker, timeouts, correlation ids.
- MCP Apps with the SDK extension, `ui/` React + Vite workspace and a placeholder bundle.
- `.env` settings (DotNetEnv + validated typed settings), `APP_PORT`, minimal JSON server info at `/`.
- Unit, integration (real MCP client, full OAuth flow against a fake IdP) and architecture tests.
- Dockerfile (optional UI build stage), Compose, CI, ADRs and guides.
