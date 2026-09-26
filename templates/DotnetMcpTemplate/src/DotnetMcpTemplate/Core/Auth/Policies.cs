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

    internal static IServiceCollection AddAppPolicies(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();

        services.AddAuthorizationBuilder()
            .AddPolicy(ProjectsWrite, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new ScopeRequirement(AppScopes.ProjectsWrite)));

        // Policies on IdP claims (roles, groups, tenant, ...): copy the claim with OIDC_TOKEN_CLAIMS, then e.g.
        //   .AddPolicy("admin", policy => policy.RequireAuthenticatedUser().RequireClaim("roles", "admin"))
        // IdPs name and shape these claims differently, so the template doesn't assume one.

        return services;
    }
}
