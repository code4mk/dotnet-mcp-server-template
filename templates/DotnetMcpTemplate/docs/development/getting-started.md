# Getting started

Requirements: .NET 10 SDK (version pinned in `global.json`). Optional: Docker, Node 22+ and pnpm
(`corepack enable`) for the MCP Apps UI.

```bash
cp .env.example .env
dotnet build
dotnet test
dotnet run --project src/DotnetMcpTemplate
```

- Server info: `http://localhost:5080/`
- MCP endpoint: `http://localhost:5080/mcp`
- Health: `http://localhost:5080/health`

The server listens on all interfaces, IPv4 and IPv6: `localhost`, `127.0.0.1` and `[::1]` all work.
Where code lives: see the tree in the [README](../../README.md) and [ADR-0003](../adr/0003-component-folders-and-service-layer.md).

## First run without an identity provider

Set `MCP_AUTH_MODE=none` in `.env` (only allowed with `APP_ENV=dev`): every request is signed in as a developer user
with the `admin` role and every scope in `Core/Auth/AppScopes.cs`. Then try it with the MCP Inspector:

```bash
npx @modelcontextprotocol/inspector
# Transport: Streamable HTTP, URL: http://localhost:5080/mcp
```

## With your identity provider

1. In your IdP, register one web application with redirect URI `http://localhost:5080/oauth/callback`
   (`${APP_URL}/oauth/callback`; change the port if you changed `APP_PORT`).
2. Set `OIDC_DISCOVERY_URL`, `OIDC_CLIENT_ID`, `OIDC_CLIENT_SECRET` (empty for a public client) in `.env`.
3. `MCP_AUTH_MODE=required`, restart, connect with a client (see connecting-clients.md): it opens the login page.

## Watch mode (local development)

```bash
dotnet watch --project src/DotnetMcpTemplate    # C# edits hot-reload; rebuilds on changes it can't apply
cd ui && pnpm run watch                          # optional, second terminal: rebuilds MCP App bundles on save
```

| You change | What happens |
| --- | --- |
| C# code (tools, services, ...) | Hot reload, or an automatic rebuild + restart (add `--non-interactive` to skip the prompt) |
| `ui/src/**` (with `pnpm run watch`) | Bundle rebuilt into `ui_dist/`; the next `resources/read` returns it, no restart |
| `.env` | Read once at startup: press **Ctrl+R** in the `dotnet watch` terminal to restart |

MCP clients reconnect on their own after a restart and stay signed in: sign-ins are kept in `.data/auth`
(`AUTH_STORE=file`). Only `AUTH_STORE=memory` loses them on restart.

## Docker

```bash
docker compose -f docker/docker-compose.yml up --build
```
