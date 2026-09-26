# ADR-0002: OIDC proxy authentication, extensible providers

| Status | Accepted |
| --- | --- |

## Decision

The default auth provider (`AUTH_PROVIDER=oidc`) makes the MCP server the OAuth 2.1 authorization server for MCP
clients and logs users in at **any OIDC identity provider** with our own client id. It validates the ID token,
merges userinfo, and issues its own short-lived JWT. Providers are pluggable through `IAuthProvider`.

## Why

- MCP clients expect dynamic client registration; most enterprise IdPs don't offer it. The proxy does.
- One configuration for every IdP: discovery URL, client id, optional secret, scopes.
- A consistent identity (`AppUser`) regardless of IdP.
- IdP tokens stay on the server (encrypted), so tools can call APIs as the user without token passthrough.

## Consequences

- The proxy holds short-lived state (registrations, logins, sessions): in memory for one instance, Redis for many.
- A consent page is required (MCP security best practices: confused deputy).
- `AUTH_TOKEN_SIGNING_KEY` is critical: it signs tokens and encrypts IdP tokens. Rotating it signs everyone out.
