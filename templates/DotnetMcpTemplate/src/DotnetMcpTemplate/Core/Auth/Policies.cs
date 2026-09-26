using Microsoft.AspNetCore.Authorization;

namespace DotnetMcpTemplate.Core.Auth;

/// <summary>
/// Authorization policy names. Use them on tools, resources and prompts:
/// <c>[Authorize(Policy = Policies.ProjectsWrite)]</c>. Plain <c>[Authorize]</c> means "any signed-in user".
/// </summary>
public static class Policies
{
    /// <summary>Requires the <c>projects:write</c> scope.</summary>
    public const string ProjectsWrite = AppScopes.ProjectsWrite;

    /// <summary>Requires the <c>admin</c> role (from OIDC_ROLE_CLAIM).</summary>
    public const string Admin = "admin";

    internal static IServiceCollection AddAppPolicies(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();

        services.AddAuthorizationBuilder()
            .AddPolicy(ProjectsWrite, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new ScopeRequirement(AppScopes.ProjectsWrite)))
            .AddPolicy(Admin, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole("admin"));

        return services;
    }
}
