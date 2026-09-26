# ADR-0003: Component folders and a service layer

| Status | Accepted |
| --- | --- |

## Decision

The project is split by how often code changes. What you write every day is at the top; plumbing is in `Core/`:

```text
src/DotnetMcpTemplate/
├── Capabilities/   Tools/, Resources/, Prompts/: what the server exposes (thin)
├── Services/       Business rules
├── Models/         Request/response DTOs
├── Integrations/   One folder per external API: typed client + its models
└── Core/           Plumbing: ApiClients (base), Auth, Mcp, Redis, Validation, Common, ServerInfo
```

Calls flow in one direction:

```text
Capabilities  →  Services  →  Integrations  →  external APIs
```

Tools, resources and prompts are thin (MCP shape, auth, validated input, one service call). Services hold business
rules and map external models to DTOs in `Models/`. Integrations only do HTTP. Namespaces follow folders. The
architecture tests enforce the direction.

## Consequences

- A new feature touches `Capabilities/`, `Services/`, `Models/` and maybe `Integrations/`; `Core/` rarely changes.
- Tests mirror the source tree (`tests/*/Capabilities/...`, `tests/*/Core/...`).
