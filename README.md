# .NET MCP Server Template

A `dotnet new` template for **Model Context Protocol servers** on ASP.NET Core (.NET 10) with the official
MCP C# SDK 2.0.

```bash
dotnet new install Code4mk.McpServer.Template
dotnet new code4mk-mcp -n NexusRE.Mcp -o nexusre-mcp
```

## What you get

| Area | Included |
| --- | --- |
| MCP | Tools, resources, prompts (all three), stateless Streamable HTTP, MCP Apps (`ui://` bundles) |
| Structure | `Capabilities/` (tools, resources, prompts), `Services/`, `Models/`, `Integrations/`; plumbing in `Core/` |
| Validation | Every tool/prompt call validated from data annotations; clear errors the model can fix |
| Auth | OIDC proxy for **any** OIDC identity provider (discovery URL + client id + optional secret + scopes), ID token + userinfo merged into one identity; `jwt` provider; custom providers via `IAuthProvider`; per-item `[Authorize]` / `[AllowAnonymous]`, scope and role policies |
| API clients | `ApiClient` base + one line per data source; auth, retries, circuit breaker, timeouts from `{PREFIX}_*` env vars |
| Settings | `.env` + validated typed settings (same pattern as dotnet-api-template), `APP_PORT`, `APP_URL` |
| Server info | `GET /` → `{ name, version, status, mcp }` |
| Tests | Unit, integration (real MCP client; full OAuth flow against a fake IdP), architecture rules |
| DevOps | Dockerfile (+ optional UI build), Compose, GitHub Actions, ADRs and guides |

## 1. Install

```bash
dotnet new install Code4mk.McpServer.Template        # from nuget.org
# or from source:
git clone https://github.com/code4mk/dotnet-mcp-template && dotnet new install ./dotnet-mcp-template/templates/DotnetMcpTemplate
```

## 2. Create a project

Run it outside this repository:

```bash
dotnet new code4mk-mcp -n NexusRE.Mcp -o nexusre-mcp
```

`-n` is the .NET name (PascalCase, no dashes), `-o` the folder / repo name. Every `DotnetMcpTemplate` becomes
`NexusRE.Mcp`; Docker names become `nexusre-mcp`; GUIDs are regenerated.

## 3. Run

```bash
cd nexusre-mcp
cp .env.example .env               # set MCP_AUTH_MODE=none for a first run without an IdP
dotnet test
dotnet run --project src/NexusRE.Mcp
npx @modelcontextprotocol/inspector   # Streamable HTTP → http://localhost:5080/mcp
```

Then connect your identity provider: register one app with redirect URI `${APP_URL}/oauth/callback` and set
`OIDC_DISCOVERY_URL`, `OIDC_CLIENT_ID`, `OIDC_CLIENT_SECRET`, `OIDC_SCOPES`. Full guides are in each project's `docs/`.

Hidden files (`.env.example`, `.gitignore`, `.github/`, `.template.config/`) start with a dot: show hidden files
in Finder (`Cmd+Shift+.`) or Explorer.

## Repository layout

```text
dotnet-mcp-template/
├── README.md, CHANGELOG.md, LICENSE
├── TemplatePack.csproj              packs the template (dotnet pack)
├── .github/workflows/               template-ci (create + build + test a project), publish (nuget.org on tags)
└── templates/DotnetMcpTemplate/     the template
```

## Maintaining

- Keep `DotnetMcpTemplate` (and `dotnetmcptemplate-slug`) as placeholders. Never create projects inside this repo.
- Versions live in `templates/DotnetMcpTemplate/Directory.Packages.props`.
- Test: `dotnet new install ./templates/DotnetMcpTemplate --force`, create a project elsewhere, `dotnet build && dotnet test`.
- Publish: update CHANGELOG, `git tag v1.1.0 && git push origin v1.1.0` (needs secret `NUGET_API_KEY`), or manually:
  `dotnet pack TemplatePack.csproj -c Release -o artifacts -p:PackageVersion=1.1.0` then `dotnet nuget push`.
