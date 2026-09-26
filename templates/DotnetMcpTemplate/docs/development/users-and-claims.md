# Users and claims

Every authenticated request carries a token, and every part of the app sees its claims as the same `AppUser`.

```text
AUTH_PROVIDER=oidc                                                   AUTH_PROVIDER=jwt
sign-in at the IdP                                                   the IdP's access token, as is
  ├─ ID token (validated)      ─┐                                      │
  └─ userinfo response         ─┴─ kept apart, and combined            │
        ▼                                                              │
  your ISignInHandler(s)         once per sign-in: call your backend,  │
                                 store SessionData, or Deny            │
        ▼                                                              │
  sub, name, email, picture, preferred_username + OIDC_TOKEN_CLAIMS    │
        ▼                                                              │
  your ITokenClaimsEnricher(s)   every token: at sign-in and refresh   │
        = the server's access token ──────────────────┬───────────────┘
                                                      ▼
                           your IClaimsTransformation(s)   (every request, any provider)
                                                      ▼
                                          HttpContext.User → AppUser
```

See every claim a user has with the `whoami` tool (MCP Inspector → Tools → `whoami`).

## ID token, userinfo and the combined view

The OIDC provider sends identity data twice: in the **ID token** (validated: signature, issuer, audience, nonce) and,
when `OIDC_USERINFO` allows, in the **userinfo response**. Both are kept exactly as received, and also combined:

| | What it is | Where |
| --- | --- | --- |
| `IdToken` | The validated ID token's claims, including `iss`, `aud`, `exp` | `SignInContext`, `TokenClaimsContext` |
| `UserInfo` | The userinfo response, or `null` when it wasn't called | `SignInContext`, `TokenClaimsContext` |
| `IdentityProviderClaims` | Both combined: **userinfo wins** for the same claim, protocol claims (`iss`, `aud`, `exp`, `nonce`, ...) removed. userinfo must be for the same `sub` | `SignInContext`, `TokenClaimsContext`; the source of the standard claims and `OIDC_TOKEN_CLAIMS` |

Use `IdToken` or `UserInfo` when the source matters (e.g. trust `email_verified` from the ID token only); use the
combined view, or `IdpValues("realm_access.roles")`, for everything else.

## Reading claims

| Where | How |
| --- | --- |
| Tool, resource, prompt | Add a parameter `AppUser user`. It's hidden from the schema, so the model can't fake it |
| Service | Take `AppUser user` as a method parameter from the tool (explicit, easy to test), or inject `AppUser` in a scoped service's constructor |
| ASP.NET Core middleware | `public async Task InvokeAsync(HttpContext context, AppUser user)`, or `context.User` |
| Minimal API endpoint | `app.MapGet("/reports", (AppUser user) => …).RequireAuthorization()` |
| MCP request filter | `AppUser.FromPrincipal(context.User)` |
| Anywhere else in a request | Inject `AppUser` (scoped), or `IHttpContextAccessor` → `HttpContext.User` |

```csharp
public Task<InvoiceDto> GetInvoice(string number, AppUser user, CancellationToken cancellationToken)
{
    var tenant = user.ClaimValues("tenant_id").FirstOrDefault();       // any claim, by name
    var canWrite = user.ClaimValues("permissions").Contains("invoices.write");
    ...
}
```

| `AppUser` member | |
| --- | --- |
| `Id`, `Name`, `Email`, `Username`, `Picture` | Standard OIDC claims (`sub`, `name`, `email`, `preferred_username`, `picture`) |
| `Scopes`, `HasScope("projects:write")` | Scopes granted to the client (`scope` or `scp`) |
| `Claims` | Every claim of the token as JSON (arrays for repeated claims) |
| `ClaimValues("roles")` | All values of one claim, `[]` when missing |
| `IsAuthenticated` | `false` for anonymous callers (`MCP_AUTH_MODE=mixed` with `[AllowAnonymous]`) |

In policies use `RequireClaim`: `policy.RequireAuthenticatedUser().RequireClaim("permissions", "invoices.write")` in
`Core/Auth/Policies.cs`, then `[Authorize(Policy = "...")]`.
See [authorization](authentication-and-authorization.md#roles-groups-and-other-idp-claims).

## At sign-in: `ISignInHandler` (oidc provider)

Runs **once per sign-in**, after the user signed in at the IdP and before the MCP client gets a token. Use it to
sync the user to your backend, fetch permissions, check a subscription, or refuse the sign-in.

```csharp
// Integrations/UserDirectory/UserDirectoryClient.cs: your backend, as a typed API client (see api-clients.md)
public sealed class UserDirectoryClient(HttpClient http) : ApiClient(http), IUserDirectoryClient
{
    public const string Prefix = "USER_DIRECTORY";   // USER_DIRECTORY_BASE_URL, USER_DIRECTORY_AUTH, ...

    public Task<DirectoryUser?> SyncAsync(SyncUserRequest request, string idpAccessToken, CancellationToken cancellationToken) =>
        PostAsync<DirectoryUser>("users/sync", request,
            new RequestHeaders { ["Authorization"] = $"Bearer {idpAccessToken}" }, cancellationToken);
}
```

```csharp
// Services/Auth/SyncUserSignInHandler.cs
public sealed class SyncUserSignInHandler(IUserDirectoryClient directory) : ISignInHandler
{
    public async ValueTask OnSignInAsync(SignInContext context, CancellationToken cancellationToken)
    {
        var user = await directory.SyncAsync(
            new SyncUserRequest(context.Subject, context.IdentityProviderClaims["email"]?.ToString(), context.UserInfo?["name"]?.ToString()),
            context.IdentityProviderAccessToken!,          // call your API as this user
            cancellationToken);

        if (user is null || !user.Active)
        {
            context.Deny("Your account has no access to this MCP server. Ask an administrator.");
            return;
        }

        // Kept with the session: every token built later (sign-in and each refresh) can use it.
        context.SessionData["internal_user_id"] = user.Id;
        context.SessionData["permissions"] = new JsonArray(user.Permissions.Select(p => (JsonNode?)p).ToArray());
    }
}
```

```csharp
// Integrations/IntegrationsSetup.cs and Services/ServicesSetup.cs
services.AddApiClient<IUserDirectoryClient, UserDirectoryClient>(configuration, UserDirectoryClient.Prefix);
services.AddScoped<ISignInHandler, SyncUserSignInHandler>();
```

- **What it sees:** `Subject`, `Issuer`, `ClientId`, `Scopes`, `IdToken`, `UserInfo`, `IdentityProviderClaims`,
  `IdpValues(path)`, and `IdentityProviderAccessToken` (the IdP's access token; what it grants depends on
  `OIDC_SCOPES`). Your API can accept that token, or authenticate the server itself
  (`USER_DIRECTORY_AUTH=client_credentials` or `api_key`) and trust the `Subject` it sends.
- **`SessionData`:** JSON kept with the session (in the auth store) and handed to every enricher. Keep it small and
  free of secrets; it's refreshed only at the next sign-in.
- **`Deny(reason)`:** the MCP client receives `access_denied` with your message; no session is created.
- **Errors:** an exception fails the sign-in with a generic `server_error` (the details are only logged).
- Several handlers run in registration order until one denies. Each runs in a DI scope.

## On every token: `ITokenClaimsEnricher` (oidc provider)

Decides, in code, what goes into the server's token. It runs every time a token is built: at sign-in **and on every
refresh**, so what it computes stays current within one access-token lifetime (`AUTH_TOKEN_LIFETIME_MINUTES`)
without signing in again.

```csharp
// Services/Auth/AppClaimsEnricher.cs
public sealed class AppClaimsEnricher(ITenantDirectory tenants) : ITokenClaimsEnricher
{
    public async ValueTask EnrichAsync(TokenClaimsContext context, CancellationToken cancellationToken)
    {
        // From the sign-in handler, without calling the backend again:
        context.Set("permissions", context.SessionValues("permissions").ToArray());

        // Map the IdP's shape to yours (dot paths read nested claims):
        //   Keycloak "realm_access.roles", Cognito "cognito:groups", Entra ID "roles", Okta "groups", ...
        context.Set("roles", context.IdpValues("realm_access.roles").ToArray());

        // Look up what must stay fresh (runs on every refresh too):
        var tenant = await tenants.FindForUserAsync(context.Subject, cancellationToken);
        if (tenant is not null)
        {
            context.Set("tenant_id", tenant.Id);
        }

        context.Remove("picture");   // or drop what you don't need
    }
}

// Services/ServicesSetup.cs
services.AddScoped<ITokenClaimsEnricher, AppClaimsEnricher>();
```

- **What it sees:** `Reason` (`SignIn` or `Refresh`), `Subject`, `ClientId`, `Scopes`, `IdToken`, `UserInfo`,
  `IdentityProviderClaims`, `SessionData`, `IdpValues(path)`, `SessionValues(path)`, and `Claims`, the claims about to
  go into the token (standard ones plus `OIDC_TOKEN_CLAIMS`).
- **Values:** string, number, bool, or an array / list (one claim per value, so `RequireClaim` works).
- **Several enrichers** run in registration order. Each runs in a DI scope, so scoped services (a `DbContext`) work.
- **Protected claims:** `sub`, `scope`, `client_id`, `sid`, `idp` and `jti` are restored after the enrichers run.
  An enricher can't change who the user is or grant scopes.
- **Keep tokens small:** clients send the token with every request. Add ids and short lists, not documents.
- **Errors:** an exception fails the sign-in or refresh. Catch what you can live without.

**Handler or enricher?** Call your backend **once** in the sign-in handler (sync, permissions) and put the result in
`SessionData`; use the enricher to shape the token and to look up only what must be fresher than one sign-in.

## Per request: `IClaimsTransformation` (any provider)

To change claims on every request, with any auth provider (including `jwt`, where the IdP issues the token), use
ASP.NET Core's `IClaimsTransformation`. `AppUser` and policies see the result.

```csharp
public sealed class PlanClaims(IPlanService plans) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity || identity.HasClaim(c => c.Type == "plan"))
        {
            return principal;
        }

        var plan = await plans.GetPlanAsync(principal.FindFirstValue("sub")!);
        identity.AddClaim(new Claim("plan", plan));
        return principal;
    }
}

// services.AddScoped<IClaimsTransformation, PlanClaims>();
```

It runs on every authenticated request, so cache anything expensive. Prefer the sign-in handler or an enricher for
values that can live in the token; use a transformation for values that must be fresh on every call.
