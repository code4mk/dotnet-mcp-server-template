# Development guides

Read top to bottom when you're new; jump to a section when you have a task.

## 1. Get running

| Guide | Covers |
| --- | --- |
| [Getting started](getting-started.md) | Requirements, first run without an IdP, [watch mode](getting-started.md#watch-mode-local-development), Docker |
| [Configuration and environments](configuration-and-environments.md) | `.env`, `APP_ENV`, typed settings (`IEnvSettings`), every main variable |
| [Connecting clients](connecting-clients.md) | Claude, VS Code, Cursor, MCP Inspector; running behind a reverse proxy |

## 2. Build MCP capabilities

| Guide | Covers |
| --- | --- |
| [Tools, resources and prompts](tools-resources-prompts.md) | Where each lives (`Capabilities/`), attributes, naming, errors, annotations |
| [Validation](validation.md) | Argument attributes, `ValidationFilter`, `IValidatableObject`, service rules |
| [MCP Apps](mcp-apps.md) | Interactive views for tools: bundle, `ui://` resource, tool link |
| [MCP Apps UI workspace](../../ui/README.md) | `ui/` layout, host connection, schemas, sandbox, building entries, adding a view |

## 3. Connect data

| Guide | Covers |
| --- | --- |
| [API clients](api-clients.md) | Typed clients in `Integrations/`, `{PREFIX}_*` settings, auth modes, resilience |

## 4. Secure it

| Guide | Covers |
| --- | --- |
| [Authentication and authorization](authentication-and-authorization.md) | Auth modes, the OIDC proxy, `jwt` provider, policies, scopes, roles, troubleshooting |
| [Custom auth providers](custom-auth-providers.md) | Implement `IAuthProvider` for anything else |

## 5. Keep it correct

| Guide | Covers |
| --- | --- |
| [Testing](testing.md) | Unit, integration (real MCP client) and architecture tests; testing auth and fakes |

## Common tasks

| I want to... | Start here |
| --- | --- |
| Add a tool | [Tools, resources and prompts → Adding a tool](tools-resources-prompts.md#adding-a-tool) |
| Call an external API | [API clients](api-clients.md), then a service in `Services/` |
| Add a setting | [Configuration → Typed settings](configuration-and-environments.md#typed-settings) |
| Require a scope or role | [Auth → Scopes](authentication-and-authorization.md#scopes) |
| Show a UI for a tool | [MCP Apps](mcp-apps.md), then [Adding a view](../../ui/README.md#adding-a-view) |
| Fix a login problem | [Auth → Default provider](authentication-and-authorization.md#default-provider-oidc-proxy-auth_provideroidc) |
| Test a tool with auth | [Testing → Integration tests](testing.md#integration-tests-the-server-as-a-client-sees-it) |

Why the code is shaped this way: [architecture decisions](../adr/).
