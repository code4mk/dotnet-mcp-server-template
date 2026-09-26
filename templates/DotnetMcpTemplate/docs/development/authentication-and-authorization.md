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

The server token contains `sub`, `name`, `email`, `picture`, `preferred_username`, the granted `scope`, and any IdP
claims listed in `OIDC_TOKEN_CLAIMS`. Inject `AppUser` to read them.

**Any MCP client can connect.** Clients register themselves (dynamic client registration); there is no allow-list.
A redirect URI is refused only when it's unsafe for OAuth: it must be `https`, `http` on a loopback host
(`localhost`, `127.0.0.1`, `[::1]`), or an app scheme such as `cursor://`, and never `javascript:`, `data:`, `file:`,
user info or a fragment. The consent page shows each new client and exactly where its credentials will be sent.

Security properties: PKCE S256 on both legs, exact redirect URI match, safe redirect URIs only, consent per client
(remembered `AUTH_CONSENT_REMEMBER_DAYS`), CSRF-protected consent,
single-use codes (5 min), rotating single-use refresh tokens, audience-bound tokens (RFC 8707), `iss` in the
authorization response (RFC 9207), codes/tokens stored by hash, IdP tokens encrypted (AES-GCM).

### Staying signed in

Users sign in once. After that the client keeps them signed in on its own:

1. **Access tokens are short-lived** (`AUTH_TOKEN_LIFETIME_MINUTES`, 60). When one expires the server answers `401`,
   and the client exchanges its refresh token at `/oauth/token` for a new pair. No login page, no consent.
2. **Refresh tokens rotate:** each is used once. Every refresh also extends the session, the refresh token and the
   client registration, so a user is only signed out after `AUTH_REFRESH_TOKEN_LIFETIME_DAYS` (30) **without using**
   the server. An active user never signs in again.
3. **Retries are safe:** for `AUTH_REFRESH_REUSE_SECONDS` (30) after a refresh, the old refresh token returns the same
   new tokens again. Two requests refreshing at once, or a retry after a lost response, don't sign the user out.
   After that window, reuse is rejected.
4. **Restarts are safe:** sign-ins are stored where a restart can't lose them.

| `AUTH_STORE` | Use for | Restarts / deploys |
| --- | --- | --- |
| `file` (default) | One instance | Kept, in `AUTH_STORE_PATH` (`.data/auth`; `/data/auth` on a volume in Docker) |
| `redis` | Several instances (`REDIS_URL` or your own connection factory, see [Redis](redis.md)) | Kept, and shared between instances |
| `memory` | Tests | **Lost: every user signs in again** |

Users are signed out when `AUTH_TOKEN_SIGNING_KEY` changes (it signs tokens and encrypts stored IdP tokens), when the
store is deleted, or after the inactivity period. Several instances: set `AUTH_STORE=redis` and `REDIS_URL`, and the
same `AUTH_TOKEN_SIGNING_KEY` everywhere.

The store holds client registrations, sessions (user claims, encrypted IdP tokens) and hashed refresh tokens: keep
`.data/` out of git (it is ignored) and back the Docker volume up like any other user data.

## Provider `jwt`

For IdPs that support MCP clients directly and issue JWT access tokens for your MCP URL: the server only validates
tokens (`OIDC_DISCOVERY_URL`, `OIDC_AUDIENCE`). The IdP's claims arrive as they are, in `AppUser.Claims`.

## Authorization

- `[Authorize]`: any signed-in user. `[AllowAnonymous]`: public (useful with `mixed`).
- Policies in `Core/Auth/Policies.cs`: `Policies.ProjectsWrite` (scope). Add your own there.
- Scopes are read from `scope` and `scp` in any format, so policies work with every IdP.

## Scopes

Scopes are defined in code, in `Core/Auth/AppScopes.cs` (`mcp:tools`, `projects:write`), and advertised in the OAuth
metadata. A client gets **only the scopes it requests**; one that requests none is signed in with no extra
permissions, so tools behind a scope policy refuse it. To add a scope:

1. Add a constant to `AppScopes` and include it in `AppScopes.All`.
2. Add a policy for it in `Policies.cs`.
3. Use it: `[Authorize(Policy = Policies.YourScope)]`.

With `MCP_AUTH_MODE=none` the developer user has every scope.

## Roles, groups and other IdP claims

Every identity provider names and shapes these differently (`roles`, `groups`, `cognito:groups`, `realm_access`,
app roles, ...), so the template maps none of them. Scopes above are the server's own and work the same with every
IdP. When you do need an IdP claim, you decide which one:

1. Copy it into the server token: `OIDC_TOKEN_CLAIMS=roles` (comma-separated, top-level claim names).
2. Require it in a policy in `Policies.cs`, or read it in code:

```csharp
.AddPolicy("admin", policy => policy.RequireAuthenticatedUser().RequireClaim("roles", "admin"))

if (user.ClaimValues("roles").Contains("admin")) { ... }
```

With the `jwt` provider the IdP's claims are already in the token; skip step 1.
