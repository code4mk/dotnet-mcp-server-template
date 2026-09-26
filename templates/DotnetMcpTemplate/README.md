# DotnetMcpTemplate

An MCP server on ASP.NET Core (.NET 10) with the official MCP C# SDK 2.0.

```bash
cp .env.example .env
dotnet run --project src/DotnetMcpTemplate
```

Connect MCP clients to `http://localhost:5080/mcp`. `GET /` returns server info.

```text
.
├── src/DotnetMcpTemplate/
│   ├── Program.cs
│   ├── Capabilities/     What the server exposes (thin): Tools/, Resources/, Prompts/
│   ├── Services/         Business logic
│   ├── Models/           Request/response DTOs
│   ├── Integrations/     One folder per external API (typed client + its models), IntegrationsSetup.cs
│   └── Core/             Plumbing you rarely touch
│       ├── ApiClients/     ApiClient base, registration, resilience, auth handlers
│       ├── Auth/           Providers (OIDC proxy for any IdP, JWT, Dev), scopes, policies, AppUser
│       ├── Mcp/            Server setup, handler registry, Filters/ (logging, errors), Apps/ (MCP App bundles)
│       ├── Validation/     Argument validation for every call
│       ├── Common/         Settings/ (.env, validated typed settings), Errors/, Middleware/
│       └── ServerInfo/     GET / server info
├── tests/                Unit, Integration (real MCP client, full OAuth flow), Architecture; mirror src/
├── ui/                   React + Vite workspace for MCP App views
├── docker/               Dockerfile, Compose
├── docs/                 ADRs (adr/) and guides (development/)
├── global.json           Pinned .NET SDK
└── Directory.*.props     Shared build settings, central package versions
```

Guides, organized by task: [docs/development/README.md](docs/development/README.md). Architecture decisions:
[docs/README.md](docs/README.md). MCP App views: [ui/README.md](ui/README.md).
