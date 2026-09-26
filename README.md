# .NET MCP Server Template

[![NuGet](https://img.shields.io/nuget/v/Code4mk.McpServer.Template?logo=nuget&label=NuGet)](https://www.nuget.org/packages/Code4mk.McpServer.Template)
[![Downloads](https://img.shields.io/nuget/dt/Code4mk.McpServer.Template?label=downloads)](https://www.nuget.org/packages/Code4mk.McpServer.Template)
[![template-ci](https://github.com/code4mk/dotnet-mcp-server-template/actions/workflows/template-ci.yml/badge.svg)](https://github.com/code4mk/dotnet-mcp-server-template/actions/workflows/template-ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/code4mk/dotnet-mcp-server-template/blob/main/LICENSE)

**A production-ready [Model Context Protocol](https://modelcontextprotocol.io) server for ASP.NET Core in one
command.** Sign-in with any identity provider, interactive UIs inside the chat, typed API clients, tests, Docker and
docs: the parts that take weeks are already done, so you write tools.

```bash
dotnet new install Code4mk.McpServer.Template
dotnet new code4mk-mcp -n Acme.Mcp -o acme-mcp
```

Built on the official [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) 2.0 and .NET 10.

## Why this template

**Remote MCP servers need real OAuth, and that's the hard part.** MCP clients such as Claude expect OAuth 2.1 with
dynamic client registration, PKCE, and protected resource metadata. Most identity providers (Entra ID, Google,
Okta, ...) don't offer dynamic registration. This template ships an **OIDC proxy** that makes any OIDC provider work
with three settings, including the consent screen, encrypted token storage and Redis for multiple instances.
**Users sign in once:** expired tokens renew silently with rotating refresh tokens, and sign-ins survive restarts and
deploys.

**Tools can show real UI, not just text.** [MCP Apps](https://github.com/modelcontextprotocol/ext-apps) are wired
end to end: React + Tailwind views that follow the host's theme, call your tools themselves (paging, drill-down),
validate every response, and preview with sample data without a server.

**The structure stays clean as it grows.** Tools → services → integrations, one direction, enforced by
architecture tests: the build fails if a tool skips auth, calls an API directly, or leaks external models.

**Errors the model can fix.** Every tool and prompt argument is validated before your code runs; the model gets a
list like `request.durationDays: High-priority projects must be 90 days or less.` and retries correctly.

| | |
| --- | --- |
| ![MCP App: projects dashboard with drill-down](https://raw.githubusercontent.com/code4mk/dotnet-mcp-server-template/main/assets/mcp-app-dashboard.png) | ![Consent page shown before a new MCP client connects](https://raw.githubusercontent.com/code4mk/dotnet-mcp-server-template/main/assets/consent-page.png) |
| An MCP App: the tool's result as a dashboard; selecting an owner calls another tool from the view | Consent before a new MCP client connects, then sign-in at your identity provider |

## Quick start

```bash
dotnet new install Code4mk.McpServer.Template
dotnet new code4mk-mcp -n Acme.Mcp -o acme-mcp
cd acme-mcp
cp .env.example .env                   # set MCP_AUTH_MODE=none for a first run without an identity provider
dotnet watch --project src/Acme.Mcp    # → Server running at http://localhost:5080
npx @modelcontextprotocol/inspector    # Streamable HTTP → http://localhost:5080/mcp
```

`-n` is the .NET name (PascalCase), `-o` the folder. Namespaces, the solution, Docker names and GUIDs follow it.

**Connect a real identity provider:** register one web app with redirect URI `http://localhost:5080/oauth/callback`,
then set `OIDC_DISCOVERY_URL`, `OIDC_CLIENT_ID` and `OIDC_CLIENT_SECRET` in `.env` and `MCP_AUTH_MODE=required`.
Add the server in Claude (Settings → Connectors → Add custom connector) and it opens your login page.

## What you get

| Area | Included |
| --- | --- |
| **MCP** | Tools, resources and prompts discovered from attributes; stateless Streamable HTTP (scales horizontally); annotations; structured content with output schemas |
| **Auth** | OIDC proxy for any OIDC provider (Entra ID, Google, Auth0, Okta, Keycloak, Zitadel, ...): DCR, PKCE, consent, ID token + userinfo merged into one `AppUser`. Or the `jwt` provider, or your own `IAuthProvider`. `[Authorize]` per item, scope and role policies |
| **MCP Apps** | `ui/` workspace (React 19, Vite, Tailwind 4, ext-apps 2): one bundle per view, host theme, tool calls from the view, zod-validated outputs, sandbox preview, per-entry builds |
| **Validation** | Data annotations and `IValidatableObject` on every tool and prompt call, nested objects included |
| **Integrations** | Typed API clients from one line of registration; retries, circuit breaker, timeouts; `bearer`, `api_key`, `basic`, `client_credentials`, or the signed-in user's token |
| **Settings** | `.env` plus typed settings classes validated at startup, every problem listed by variable name |
| **Dev loop** | `dotnet watch` with hot reload, `pnpm run watch` for views, dual-stack `localhost`, startup URL in the log |
| **Tests** | Unit, integration (a real MCP client against the in-memory server, including the full OAuth flow against a fake IdP) and architecture rules: 64 tests out of the box |
| **Ship** | Multi-stage Dockerfile, Compose, GitHub Actions (build, test, UI bundle check, Docker), pinned SDK, central package versions |
| **Docs** | 10 guides organized by task, 4 architecture decision records |

## Project layout

```text
acme-mcp/
├── src/Acme.Mcp/
│   ├── Capabilities/       Tools/, Resources/, Prompts/: what the server exposes (thin)
│   ├── Services/           Business logic
│   ├── Models/             Request / response DTOs
│   ├── Integrations/       One folder per external API
│   ├── Core/               Auth, MCP setup and filters, validation, settings, errors: plumbing you rarely touch
│   └── ui_dist/            Built MCP App bundles
├── ui/                     MCP App views (React + Vite + Tailwind)
├── tests/                  Unit, Integration, Architecture (mirror src/)
├── docs/                   Guides and ADRs
└── docker/                 Dockerfile, Compose
```

Adding a tool is one class; there is no registration line:

```csharp
[McpServerToolType]
[Authorize]
public sealed class InvoiceTools(IInvoiceService invoices)
{
    [McpServerTool(Name = "get_invoice", ReadOnly = true, UseStructuredContent = true)]
    [Description("Gets an invoice by number.")]
    public Task<InvoiceDto> GetInvoice(
        [Required, RegularExpression("^INV-[0-9]{6}$")] string number,
        AppUser user,
        CancellationToken cancellationToken) =>
        invoices.GetAsync(number, user, cancellationToken);
}
```

## Works with

- **Clients:** Claude (claude.ai and Desktop), VS Code, Cursor, MCP Inspector, and any client that speaks Streamable HTTP with OAuth.
- **Identity providers:** anything with an OpenID Connect discovery URL.
- **Hosting:** anywhere a container runs; set `APP_URL` behind a reverse proxy.

## Documentation

Each generated project includes its docs. Browse them on GitHub:

- [Development guides](https://github.com/code4mk/dotnet-mcp-server-template/blob/main/templates/DotnetMcpTemplate/docs/development/README.md): getting started, configuration, tools, validation, MCP Apps, API clients, auth, testing, connecting clients
- [MCP Apps UI workspace](https://github.com/code4mk/dotnet-mcp-server-template/blob/main/templates/DotnetMcpTemplate/ui/README.md)
- [Architecture decisions](https://github.com/code4mk/dotnet-mcp-server-template/blob/main/templates/DotnetMcpTemplate/docs/README.md)
- [Changelog](https://github.com/code4mk/dotnet-mcp-server-template/blob/main/CHANGELOG.md)

**Requirements:** .NET 10 SDK. Optional: Docker; Node 22+ and pnpm for MCP App views.

## Contributing

Issues and pull requests are welcome. To work on the template itself:

```bash
git clone https://github.com/code4mk/dotnet-mcp-server-template
dotnet new install ./dotnet-mcp-server-template/templates/DotnetMcpTemplate --force
dotnet new code4mk-mcp -n Sample.Mcp -o ../sample-mcp     # always create projects outside the repo
```

- `DotnetMcpTemplate` (and `dotnetmcptemplate-slug`) are placeholders; keep them. Package versions live in
  `templates/DotnetMcpTemplate/Directory.Packages.props`.
- CI creates a project from the template, builds and tests it, checks the UI bundles, and packs the template.
- Changing, testing and publishing the template: [nuget-publish.md](https://github.com/code4mk/dotnet-mcp-server-template/blob/main/nuget-publish.md).

## License

[MIT](https://github.com/code4mk/dotnet-mcp-server-template/blob/main/LICENSE) © Code4mk
