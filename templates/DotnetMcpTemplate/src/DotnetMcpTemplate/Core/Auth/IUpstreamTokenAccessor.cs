namespace DotnetMcpTemplate.Core.Auth;

/// <summary>
/// Gives API clients the signed-in user's access token from the identity provider (used by <c>{PREFIX}_AUTH=user</c>).
/// This is NOT the token the MCP client sent (forwarding that is forbidden by the MCP spec): it is the IdP token the
/// server obtained for its own client, stored encrypted on the server. Only the oidc provider supports it.
/// </summary>
public interface IUpstreamTokenAccessor
{
    /// <summary>The user's IdP access token (refreshed when needed), or null when unavailable.</summary>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken);
}

internal sealed class NoUpstreamTokenAccessor : IUpstreamTokenAccessor
{
    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken) => Task.FromResult<string?>(null);
}
