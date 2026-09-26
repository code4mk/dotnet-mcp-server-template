# Configuration and environments

All configuration comes from environment variables, usually from `.env` at the repository root
(same pattern as dotnet-api-template). `appsettings.json` only holds logging levels.

- `.env.example` is committed and lists every variable. `.env` and `.env.*` are ignored by git.
- Precedence: real environment variables (Docker, CI, shell) → `.env` → defaults in the settings classes.
- `APP_ENV` (`dev` | `stage` | `prod`) sets the ASP.NET Core environment.

## Typed settings

A class marked `IEnvSettings` is registered and validated automatically at startup, like pydantic `BaseSettings`:

```csharp
public sealed class PaymentSettings : IEnvSettings
{
    [ConfigurationKeyName("PAYMENTS_BASE_URL")]
    [Required, Url]
    public string BaseUrl { get; init; } = string.Empty;
}
```

Inject it directly (`PaymentSettings settings`). Startup fails with every problem listed by variable name.
Settings that only apply to one auth provider (`OidcSettings`, `OidcProxySettings`, `JwtProviderSettings`) or one
API client are not marked; they are registered by that provider/client, so unused ones need no values.

## Main variables

| Variable | Default | Purpose |
| --- | --- | --- |
| `APP_ENV` | `dev` | Environment |
| `APP_PORT` | `5080` | Listening port (also Docker) |
| `APP_URL` | `http://localhost:${APP_PORT}` | Public URL; token audience and issuer. Set it behind a proxy |
| `MCP_SERVER_NAME`, `MCP_PATH`, `MCP_INSTRUCTIONS` | | MCP server identity and endpoint |
| `MCP_AUTH_MODE` | `required` | `required` / `mixed` / `none` (dev) |
| `MCP_UI_DIST_DIR` | `ui_dist` next to the app | MCP App bundles |
| `AUTH_PROVIDER` | `oidc` | `oidc`, `jwt` or a custom provider |
| `OIDC_*` | | Identity provider (see authentication-and-authorization.md) |
| `AUTH_*` | | OAuth proxy: signing key, lifetimes, redirect URIs, store |
| `{PREFIX}_*` | | One set per API client (see api-clients.md) |
