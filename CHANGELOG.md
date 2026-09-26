# Changelog

## [1.0.0] - 2026-09-27

First release.

### Added

- **MCP server** on ASP.NET Core (.NET 10) with ModelContextProtocol.AspNetCore 2.0: stateless Streamable HTTP;
  tools, resources (templated, direct, MCP App `ui://`) and prompts discovered from attributes; structured content.
- **Layout:** `Capabilities/` (tools, resources, prompts), `Services/`, `Models/`, `Integrations/` (external API
  clients), `Core/` (auth, MCP setup and filters, validation, settings, errors, server info). Namespaces follow
  folders; tests mirror the source tree; architecture tests enforce the call direction and auth on every item.
- **Validation** of every tool and prompt call (data annotations, nested objects, `IValidatableObject`), returned as
  errors the model can fix.
- **Auth:** OIDC proxy for any OIDC identity provider (dynamic client registration, PKCE, ID token + userinfo merge,
  rotating refresh tokens, encrypted IdP tokens, memory or Redis store), consent page in the style of the FastMCP
  OAuth proxy, `jwt` resource-server provider, extensible `IAuthProvider`, dev mode. Scopes are defined in code
  (`Core/Auth/AppScopes.cs`); clients get only the scopes they request.
- **MCP Apps UI** (`ui/`): ext-apps 2, React 19, Vite 8, Tailwind 4. One folder per view, shared host connection
  (`useToolOutput`, `useCallTool`, `useMcpApp`), host theme tokens, zod-validated tool outputs, failed / cancelled
  results shown in the view, dev sandbox with sample data (left out of production bundles), per-entry or full
  builds. Sample `projects-dashboard` with drill-down through `list_projects`.
- **Typed API clients** (`ApiClient` base) with env-configured auth (`bearer`, `api_key`, `basic`,
  `client_credentials`, user token), retries, circuit breaker, timeouts, correlation ids.
- **Settings:** `.env` (DotNetEnv) plus typed settings validated at startup; `APP_PORT`, `APP_URL`; dual-stack
  listening (`localhost`, `127.0.0.1`, `[::1]`); startup logs the public URL; minimal JSON server info at `/`.
- **Dev loop:** `dotnet watch` and `pnpm run watch`; rebuilt MCP App bundles are served without a restart.
- **Tests:** unit, integration (a real MCP client, including the full OAuth flow against a fake IdP) and
  architecture tests.
- **Ship:** multi-stage Dockerfile (optional UI build), Compose, GitHub Actions (build, test, UI bundle check,
  Docker), `global.json`, central package management.
- **Docs:** 10 guides indexed by task (`docs/development/README.md`) and 4 ADRs.
