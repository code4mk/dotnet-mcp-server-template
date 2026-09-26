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

Guides: [getting started](docs/development/getting-started.md), [configuration](docs/development/configuration-and-environments.md),
[tools, resources, prompts](docs/development/tools-resources-prompts.md), [validation](docs/development/validation.md),
[auth](docs/development/authentication-and-authorization.md), [custom providers](docs/development/custom-auth-providers.md),
[API clients](docs/development/api-clients.md), [MCP Apps](docs/development/mcp-apps.md), [clients](docs/development/connecting-clients.md).
All docs and ADRs: [docs/README.md](docs/README.md).
