namespace DotnetMcpTemplate.Core.Auth.Providers;

/// <summary>
/// Extension point for authentication. Implement it in any loaded assembly, give it a unique <see cref="Name"/>
/// and select it with AUTH_PROVIDER. It needs a public parameterless constructor.
/// </summary>
/// <remarks>
/// A provider registers the scheme that validates bearer tokens on the MCP endpoint and says which
/// authorization server(s) clients must use. The template adds the MCP challenge and the OAuth protected resource
/// metadata on top, so providers don't need to know about MCP. See docs/development/custom-auth-providers.md.
/// </remarks>
public interface IAuthProvider
{
    /// <summary>Value of AUTH_PROVIDER that selects this provider (case-insensitive).</summary>
    string Name { get; }

    /// <summary>Registers services and the bearer scheme. Set <see cref="AuthProviderContext.BearerScheme"/>.</summary>
    void Configure(AuthProviderContext context);

    /// <summary>Maps provider endpoints (login, callback, token, ...), if any.</summary>
    void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
