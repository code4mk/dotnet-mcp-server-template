# Authentication and authorization

## Modes (`MCP_AUTH_MODE`)

- `required` (default): the whole MCP endpoint needs a token. Clients start the login when they connect.
- `mixed`: anonymous clients can connect; `[Authorize]` is checked per tool/resource/prompt, and items the user
  can't use are hidden from lists.
- `none`: a developer user is signed in automatically. Refused unless `APP_ENV=dev`.

## Default provider: OIDC proxy (`AUTH_PROVIDER=oidc`)

Works with any OIDC IdP (Google, Entra ID, Auth0, Okta, Keycloak, Zitadel, Authentik, ...). Configure:

```bash
OIDC_DISCOVERY_URL=https://login.example.com/.well-known/openid-configuration
OIDC_CLIENT_ID=...
OIDC_CLIENT_SECRET=...          # empty = public client
OIDC_SCOPES=openid,profile,email
```

Register one app at the IdP (type: regular web application) with redirect / callback URL
`${APP_URL}/oauth/callback`, e.g. `http://localhost:5080/oauth/callback`, not `APP_URL` alone.

If login fails with `IDX20803: Unable to obtain configuration`, open `OIDC_DISCOVERY_URL` in a browser: it must return
JSON and the path is `/.well-known/openid-configuration` (with the leading dot).

Flow: the MCP client gets `401` + `resource_metadata` → reads `/.well-known/oauth-protected-resource/mcp` and
`/.well-known/oauth-authorization-server` → registers at `/oauth/register` → `/oauth/authorize` (PKCE) → consent page →
IdP login → `/oauth/callback` (the server validates the ID token, calls userinfo, merges claims, stores the IdP tokens
encrypted) → the client exchanges the code at `/oauth/token` for the **server's** token.

The server token contains `sub`, `name`, `email`, `picture`, `preferred_username`, `roles` (from `OIDC_ROLE_CLAIM`,
dot paths allowed), `groups`, granted `scope`, and any `OIDC_TOKEN_CLAIMS`. Inject `AppUser` to read them.

Security properties: PKCE S256 on both legs, exact redirect URI match, registration limited to
`AUTH_ALLOWED_REDIRECT_URIS`, consent per client (remembered `AUTH_CONSENT_REMEMBER_DAYS`), CSRF-protected consent,
single-use codes (5 min), rotating single-use refresh tokens, audience-bound tokens (RFC 8707), `iss` in the
authorization response (RFC 9207), codes/tokens stored by hash, IdP tokens encrypted (AES-GCM).

Several instances: set `AUTH_STORE=redis` and `REDIS_URL`, and the same `AUTH_TOKEN_SIGNING_KEY` everywhere.

## Provider `jwt`

For IdPs that support MCP clients directly and issue JWT access tokens for your MCP URL: the server only validates
tokens (`OIDC_DISCOVERY_URL`, `OIDC_AUDIENCE`, `OIDC_ROLE_CLAIM`).

## Authorization

- `[Authorize]`: any signed-in user. `[AllowAnonymous]`: public (useful with `mixed`).
- Policies in `Core/Auth/Policies.cs`: `Policies.ProjectsWrite` (scope), `Policies.Admin` (role). Add your own there.
- Scopes are read from `scope` and `scp` in any format, so policies work with every IdP.

## Scopes

Scopes are defined in code, in `Core/Auth/AppScopes.cs` (`mcp:tools`, `projects:write`), and advertised in the OAuth
metadata. A client gets **only the scopes it requests**; one that requests none is signed in with no extra
permissions, so tools behind a scope policy refuse it. To add a scope:

1. Add a constant to `AppScopes` and include it in `AppScopes.All`.
2. Add a policy for it in `Policies.cs`.
3. Use it: `[Authorize(Policy = Policies.YourScope)]`.

Roles (`[Authorize(Roles = ...)]`, `Policies.Admin`) come from the IdP through `OIDC_ROLE_CLAIM`, not from scopes.
With `MCP_AUTH_MODE=none` the developer user has every scope and the `admin` role.
