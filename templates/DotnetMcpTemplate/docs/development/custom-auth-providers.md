# Custom auth providers

Implement `IAuthProvider` in this project or any referenced class library, with a parameterless constructor.
It is discovered automatically; select it with `AUTH_PROVIDER=<name>`.

```csharp
public sealed class ApiKeyAuthProvider : IAuthProvider
{
    public string Name => "api-key";

    public void Configure(AuthProviderContext context)
    {
        context.Services.AddEnvSettings<ApiKeySettings>(context.Configuration);
        context.Authentication.AddScheme<AuthenticationSchemeOptions, ApiKeyHandler>("ApiKey", _ => { });
        context.BearerScheme = "ApiKey";                    // validates requests on the MCP endpoint
        context.AuthorizationServers.Add(context.PublicUrl); // advertised in protected resource metadata
    }

    // Optional: map login/callback endpoints.
    public void MapEndpoints(IEndpointRouteBuilder endpoints) { }
}
```

The template adds the MCP 401 challenge, the protected resource metadata, policies and `AppUser` on top.
Write claims with the names in `AppClaims` (`sub`, `name`, `email`, `roles`, `scope`) so `AppUser` and policies work.

Examples in the repository: `Core/Auth/Jwt/JwtAuthProvider.cs` (resource server) and
`tests/.../TestInfrastructure/TestAuthProvider.cs` (header-based, for tests).

To call APIs as the user with a custom provider, also register an `IUpstreamTokenAccessor`.
