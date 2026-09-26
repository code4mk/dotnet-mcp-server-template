namespace DotnetMcpTemplate.Services;

public static class ServicesSetup
{
    /// <summary>Business services. Scoped: one instance per MCP request.</summary>
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ISearchService, SearchService>();

        // Sign-in and token hooks (docs/development/users-and-claims.md):
        // services.AddScoped<ISignInHandler, SyncUserSignInHandler>();    // once per sign-in: sync the user, fetch permissions, deny
        // services.AddScoped<ITokenClaimsEnricher, AppClaimsEnricher>(); // every token: map IdP roles, add a tenant
        return services;
    }
}
