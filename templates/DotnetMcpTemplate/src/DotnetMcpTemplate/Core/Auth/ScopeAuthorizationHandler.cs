using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace DotnetMcpTemplate.Core.Auth;

public sealed record ScopeRequirement(string Scope) : IAuthorizationRequirement;

/// <summary>
/// Checks scopes the way every IdP writes them: <c>scope</c> (space-separated string, most IdPs) and
/// <c>scp</c> (Entra ID), as one claim or repeated claims.
/// </summary>
public sealed class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeRequirement requirement)
    {
        if (ReadScopes(context.User).Contains(requirement.Scope, StringComparer.Ordinal))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    public static IReadOnlyList<string> ReadScopes(ClaimsPrincipal principal) =>
        principal.FindAll(c => c.Type is AppClaims.Scope or AppClaims.EntraScope)
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}
