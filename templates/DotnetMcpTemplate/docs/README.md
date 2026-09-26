# Documentation

## Guides (`development/`)

| Guide | Read it when you... |
| --- | --- |
| [Getting started](development/getting-started.md) | Run the server for the first time |
| [Configuration and environments](development/configuration-and-environments.md) | Add or change a setting |
| [Tools, resources and prompts](development/tools-resources-prompts.md) | Add an MCP capability |
| [Validation](development/validation.md) | Validate tool or prompt arguments |
| [API clients](development/api-clients.md) | Call an external API from a service |
| [Authentication and authorization](development/authentication-and-authorization.md) | Set up login, scopes, roles |
| [Custom auth providers](development/custom-auth-providers.md) | Plug in your own auth |
| [MCP Apps](development/mcp-apps.md) | Show an interactive UI from a tool |
| [Connecting clients](development/connecting-clients.md) | Connect Claude, VS Code, Cursor, Inspector |

## Architecture decisions (`adr/`)

| ADR | Decision |
| --- | --- |
| [0001](adr/0001-mcp-csharp-sdk-over-http.md) | Official MCP C# SDK over stateless Streamable HTTP |
| [0002](adr/0002-oidc-proxy-authentication.md) | OIDC proxy authentication, extensible providers |
| [0003](adr/0003-component-folders-and-service-layer.md) | Folder layout and the call direction |

Add an ADR (`adr/NNNN-short-title.md`, same format) for any decision a new team member would otherwise ask about.
