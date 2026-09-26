# DotnetMcpTemplate

A [Model Context Protocol](https://modelcontextprotocol.io) server on ASP.NET Core (.NET 10) with the official MCP C#
SDK 2.0. It exposes tools, resources and prompts over Streamable HTTP, signs users in through any OpenID Connect
identity provider, and renders interactive views in the chat with MCP Apps.

- [Quick start](#quick-start)
- [Endpoints](#endpoints)
- [Configuration](#configuration)
- [Authentication](#authentication)
- [Connecting clients](#connecting-clients)
- [MCP App UI](#mcp-app-ui)
- [Project structure](#project-structure)
- [Development](#development)
- [Testing](#testing)
- [Docker](#docker)
- [Documentation](#documentation)
- [Troubleshooting](#troubleshooting)

## Quick start

**Requirements:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (version pinned in `global.json`).
Optional: Docker; Node 22+ with pnpm (`corepack enable`) for the MCP App UI.

```bash
cp .env.example .env
```

Set `MCP_AUTH_MODE=none` in `.env` for a first run without an identity provider (only allowed with `APP_ENV=dev`),
then start the server:

```bash
dotnet run --project src/DotnetMcpTemplate
# info: Server running at http://localhost:5080
```

Try it with the MCP Inspector:

```bash
npx @modelcontextprotocol/inspector
# Transport: Streamable HTTP · URL: http://localhost:5080/mcp · Connect → Tools → List Tools
```

## Endpoints

| Path | Purpose |
| --- | --- |
| `/mcp` | MCP endpoint (Streamable HTTP, stateless). Change with `MCP_PATH` |
| `/` | Server info: `{ name, version, status, mcp }` |
| `/health` | Health check for load balancers and orchestrators |
| `/.well-known/oauth-protected-resource/mcp` | Protected resource metadata (tells clients where to sign in) |
| `/.well-known/oauth-authorization-server` | OAuth metadata of the built-in OIDC proxy |
| `/oauth/*` | OIDC proxy: register, authorize, consent, callback, token |

## Configuration

All settings are environment variables, usually from `.env` (never committed; `.env.example` lists every variable).
Real environment variables (Docker, CI, shell) take precedence. Settings are validated at startup and every problem
is reported by variable name.

| Variable | Default | Purpose |
| --- | --- | --- |
| `APP_ENV` | `dev` | `dev`, `stage` or `prod` |
| `APP_PORT` | `5080` | Listening port (IPv4 and IPv6: `localhost`, `127.0.0.1`, `[::1]`) |
| `APP_URL` | `http://localhost:{APP_PORT}` | Public URL; set it behind a reverse proxy |
| `MCP_SERVER_NAME`, `MCP_PATH`, `MCP_INSTRUCTIONS` | | Server identity and endpoint |
| `MCP_AUTH_MODE` | `required` | `required`, `mixed` or `none` (dev only) |
| `AUTH_PROVIDER` | `oidc` | `oidc`, `jwt` or a custom provider |
| `OIDC_*` | | Identity provider: discovery URL, client id and secret, scopes, claims |
| `AUTH_*` | | Token signing key and lifetimes, allowed redirect URIs, sign-in store (`file`, `redis`, `memory`) |
| `SAMPLE_API_*` | | Example external API client; one `{PREFIX}_*` set per API |

Details: [configuration guide](docs/development/configuration-and-environments.md).

## Authentication

With `AUTH_PROVIDER=oidc` the server is the OAuth authorization server for MCP clients and signs users in at your
identity provider (Entra ID, Google, Auth0, Okta, Keycloak, Zitadel, ...):

1. Register one web application at the identity provider with the redirect URI
   `http://localhost:5080/oauth/callback` (in general `${APP_URL}/oauth/callback`).
2. Set `OIDC_DISCOVERY_URL`, `OIDC_CLIENT_ID` and `OIDC_CLIENT_SECRET` in `.env`, and `MCP_AUTH_MODE=required`.
3. Connect a client: it registers, shows the consent page, and sends you to the identity provider to sign in.

Tools, resources and prompts declare `[Authorize]` or `[AllowAnonymous]`. Scopes are defined in
`Core/Auth/AppScopes.cs` and policies in `Core/Auth/Policies.cs`.

**Users sign in once.** Expired access tokens are renewed with rotating refresh tokens, every refresh extends the
session, and sign-ins are stored in `.data/auth` (`AUTH_STORE=file`), so restarts and deploys don't sign anyone out.
For multiple instances set `AUTH_STORE=redis`, `REDIS_URL` and the same `AUTH_TOKEN_SIGNING_KEY` everywhere. See
[staying signed in](docs/development/authentication-and-authorization.md#staying-signed-in).

Details: [authentication and authorization](docs/development/authentication-and-authorization.md).

## Connecting clients

The server URL is `APP_URL` + `MCP_PATH`, for example `https://mcp.example.com/mcp`.

| Client | How |
| --- | --- |
| Claude (claude.ai, Desktop) | Settings → Connectors → Add custom connector → the URL |
| VS Code | `.vscode/mcp.json`: `{ "servers": { "my-server": { "type": "http", "url": "…/mcp" } } }` |
| Cursor | `~/.cursor/mcp.json`: `{ "mcpServers": { "my-server": { "url": "…/mcp" } } }` |
| MCP Inspector | `npx @modelcontextprotocol/inspector` → Streamable HTTP → the URL |

A client's redirect URI must match `AUTH_ALLOWED_REDIRECT_URIS`. Details: [connecting clients](docs/development/connecting-clients.md).

## MCP App UI

MCP Apps let a tool render an interactive view inside the conversation. The views live in `ui/` (React 19, Vite,
Tailwind 4, `@modelcontextprotocol/ext-apps` 2). Each view builds to one self-contained HTML file in
`src/DotnetMcpTemplate/ui_dist/`, which the server serves as a `ui://` resource. The built bundles are committed, so
the server runs without Node; you only need the UI toolchain to change a view.

The sample view `projects-dashboard` belongs to the `show_projects_dashboard` tool. It renders the tool's result and
loads an owner's projects by calling `list_projects` from the view.

### Setup

```bash
corepack enable        # once per machine: provides pnpm
cd ui
pnpm install
```

### Develop

**Sandbox** (no server, no host, no login). Each view renders with sample data from `ui/src/apps/<entry>/sandbox.ts`,
including the tools it calls, so drill-down, paging and loading states all work:

```bash
cd ui
pnpm dev               # http://localhost:5173: sandbox index; open a view from there
```

Edits hot-reload in the browser. This is the fastest loop for layout and interaction work.

**With the server** (real data, real tool calls). Run the server and the UI watcher side by side:

```bash
# terminal 1: server with hot reload
dotnet watch --project src/DotnetMcpTemplate

# terminal 2: rebuild bundles on save (all views, or name one)
cd ui && pnpm run watch                      # or: pnpm run watch projects-dashboard
```

In development the server reads bundles straight from `src/DotnetMcpTemplate/ui_dist/`, so each rebuild is served on
the next `resources/read` without a restart. Open the view from an MCP client that supports MCP Apps by asking it to
run the tool (e.g. "show the projects dashboard").

### Build

```bash
cd ui
pnpm run build                        # typecheck + build every view
pnpm run build projects-dashboard     # typecheck + build only the named views
pnpm run entries                      # list the views
pnpm run typecheck                    # typecheck only
```

Output: `src/DotnetMcpTemplate/ui_dist/<entry>.html`. **Commit it with your UI changes.** Builds are reproducible,
and CI fails when the committed bundles don't match the source. The Docker image rebuilds the UI from
`ui/pnpm-lock.yaml` as well. Production bundles contain no sample data: opening one directly shows
"Open this view in an MCP host".

### Add a view

1. Create `ui/src/entries/<entry>.html` and `<entry>.tsx` (copy the `projects-dashboard` pair). Names are kebab-case.
2. Create `ui/src/apps/<entry>/`: `schema.ts` (zod schemas mirroring the C# response records), `sandbox.ts`,
   `lib/use<Entry>.ts`, `components/`, `App.tsx`.
3. Add a resource in `Capabilities/Resources/UiResources.cs` that returns `bundles.Read("<entry>")` as
   `ui://<entry>`, and put `[McpAppUi(ResourceUri = "ui://<entry>")]` and `UseStructuredContent = true` on the tool.
4. `pnpm run build <entry>` and commit the bundle.

Patterns (host connection, schemas, errors, theming): [ui/README.md](ui/README.md) ·
[MCP Apps guide](docs/development/mcp-apps.md).

## Project structure

```text
.
├── src/DotnetMcpTemplate/
│   ├── Program.cs
│   ├── Capabilities/     What the server exposes (thin): Tools/, Resources/, Prompts/
│   ├── Services/         Business logic
│   ├── Models/           Request / response DTOs
│   ├── Integrations/     One folder per external API (typed client + its models), IntegrationsSetup.cs
│   ├── Core/             Plumbing you rarely touch
│   │   ├── ApiClients/     ApiClient base, registration, resilience, auth handlers
│   │   ├── Auth/           Providers (OIDC proxy, JWT, dev), scopes, policies, AppUser
│   │   ├── Mcp/            Server setup, handler registry, Filters/ (logging, errors), Apps/ (bundles)
│   │   ├── Validation/     Argument validation for every call
│   │   ├── Common/         Settings/ (.env, typed settings), Errors/, Middleware/
│   │   └── ServerInfo/     GET /
│   └── ui_dist/          Built MCP App bundles (committed)
├── ui/                   MCP App views: entries/, apps/<entry>/, shared/
├── tests/                Unit, Integration, Architecture (mirror src/)
├── docs/                 Guides (development/) and architecture decisions (adr/)
├── docker/               Dockerfile, Compose
├── global.json           Pinned .NET SDK
└── Directory.*.props     Shared build settings, central package versions
```

Calls flow one way: **Capabilities → Services → Integrations → external APIs**. Architecture tests enforce it.

## Development

```bash
dotnet watch --project src/DotnetMcpTemplate   # hot reload; press Ctrl+R to restart after editing .env
```

Adding a tool is one class; tools are discovered automatically:

```csharp
[McpServerToolType]
[Authorize]
public sealed class InvoiceTools(IInvoiceService invoices)
{
    [McpServerTool(Name = "get_invoice", ReadOnly = true, UseStructuredContent = true)]
    [Description("Gets an invoice by number.")]
    public Task<InvoiceDto> GetInvoice(
        [Required, RegularExpression("^INV-[0-9]{6}$"), Description("Invoice number, e.g. INV-000123.")] string number,
        AppUser user,
        CancellationToken cancellationToken) =>
        invoices.GetAsync(number, user, cancellationToken);
}
```

Keep tools thin: validate input with attributes, call one service, return a DTO. Business rules go in `Services/`,
external calls in `Integrations/`. Throw `AppException.NotFound/Conflict/Rule(...)` for expected errors; the model
receives the message. See [tools, resources and prompts](docs/development/tools-resources-prompts.md).

## Testing

```bash
dotnet test                                    # unit, integration and architecture tests
dotnet test --filter "FullyQualifiedName~ToolTests"
cd ui && pnpm run typecheck                    # MCP App views
```

Integration tests start the server in memory and call it through a real MCP client, including the complete OAuth
flow against a fake identity provider. Architecture tests fail the build when a tool skips `[Authorize]` /
`[AllowAnonymous]`, calls an API client directly, or a service leaks external models.
See [testing](docs/development/testing.md).

## Docker

```bash
docker compose -f docker/docker-compose.yml up --build                   # local, uses .env
docker build -f docker/Dockerfile -t dotnetmcptemplate-slug .            # image only
```

The image builds the MCP App bundles when `ui/pnpm-lock.yaml` exists, then runs the server on `APP_PORT` over plain
HTTP. In production, terminate TLS at a reverse proxy and set `APP_URL` to the public HTTPS URL.

## Documentation

| | |
| --- | --- |
| [Development guides](docs/development/README.md) | Every guide, organized by task: run, build, connect, secure, test |
| [MCP App UI workspace](ui/README.md) | Layout and patterns of the `ui/` workspace |
| [Architecture decisions](docs/README.md) | Why the code is shaped this way (ADRs) |

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| Settings error at startup | The message names each variable; compare `.env` with `.env.example` |
| `MCP_AUTH_MODE=none` is refused | Only allowed with `APP_ENV=dev` |
| Login fails with `IDX20803: Unable to obtain configuration` | Open `OIDC_DISCOVERY_URL` in a browser: it must return JSON (note the dot in `/.well-known/`) |
| Identity provider says the redirect URI is invalid | Register `${APP_URL}/oauth/callback` exactly, not `APP_URL` alone |
| A client can't register (`invalid_redirect_uri`) | Add its redirect URI pattern to `AUTH_ALLOWED_REDIRECT_URIS` |
| Users must sign in again after a restart | `AUTH_STORE=memory` loses sign-ins: use `file` (default) or `redis`; in Docker keep the `/data` volume |
| `.env` changes have no effect under `dotnet watch` | Settings are read at startup: press Ctrl+R in the watch terminal |
| `UI bundle not found at …/ui_dist/<entry>.html` | `cd ui && pnpm install && pnpm run build` |
| CI fails on "Committed bundles match the source" | `cd ui && pnpm run build`, then commit `src/DotnetMcpTemplate/ui_dist/` |
