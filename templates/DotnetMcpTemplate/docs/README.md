# Documentation

- **[Development guides](development/README.md)**: how to run, build, connect, secure and test the server,
  organized by task.
- **[UI workspace](../ui/README.md)**: building MCP App views.
- **Architecture decisions** (`adr/`): why the code is shaped this way.

| ADR | Decision |
| --- | --- |
| [0001](adr/0001-mcp-csharp-sdk-over-http.md) | Official MCP C# SDK over stateless Streamable HTTP |
| [0002](adr/0002-oidc-proxy-authentication.md) | OIDC proxy authentication, extensible providers |
| [0003](adr/0003-component-folders-and-service-layer.md) | Folder layout and the call direction |
| [0004](adr/0004-mcp-apps-ui-architecture.md) | MCP Apps UI: one bundle per entry, validated tool outputs, sandbox |

Add an ADR (`adr/NNNN-short-title.md`, same format) for any decision a new team member would otherwise ask about.
