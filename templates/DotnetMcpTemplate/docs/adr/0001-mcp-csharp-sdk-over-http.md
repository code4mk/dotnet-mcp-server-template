# ADR-0001: Official MCP C# SDK over Streamable HTTP

| Status | Accepted |
| --- | --- |

## Decision

MCP servers are ASP.NET Core apps using the official **ModelContextProtocol.AspNetCore 2.x** SDK over Streamable HTTP,
**stateless** (the SDK 2.0 default), with tools, resources and prompts declared by attributes and discovered from the
assembly. MCP Apps use the SDK's `ModelContextProtocol.Extensions.Apps` package.

## Why

- First-party, maintained with Microsoft, aligned with the 2026-07-28 spec, down-level compatible with older clients.
- Stateless HTTP scales horizontally behind a load balancer with no sticky sessions.
- Attributes keep each tool/resource/prompt self-describing; the same attributes drive schema, auth and validation.

## Consequences

- No server-initiated requests (sampling, elicitation via sessions); use multi-round-trip requests when needed.
- MCP Apps is experimental in SDK 2.0 (`MCPEXP003` is suppressed in Directory.Build.props).
