using Microsoft.AspNetCore.Authentication;

namespace DotnetMcpTemplate.Core.Auth.Providers;

public sealed class AuthProviderContext(
    IServiceCollection services,
    IConfiguration configuration,
    AuthenticationBuilder authentication,
    string publicUrl,
    string resourceUrl)
{
    public IServiceCollection Services { get; } = services;

    public IConfiguration Configuration { get; } = configuration;

    /// <summary>Add authentication schemes here, e.g. <c>Authentication.AddJwtBearer(...)</c>.</summary>
    public AuthenticationBuilder Authentication { get; } = authentication;

    /// <summary>Public base URL of this server (APP_URL), no trailing slash.</summary>
    public string PublicUrl { get; } = publicUrl;

    /// <summary>The MCP endpoint URL (APP_URL + MCP_PATH): the token audience (RFC 8707).</summary>
    public string ResourceUrl { get; } = resourceUrl;

    /// <summary>Required: the scheme that authenticates bearer tokens on the MCP endpoint.</summary>
    public string? BearerScheme { get; set; }

    /// <summary>Authorization server URL(s) advertised to MCP clients in the protected resource metadata.</summary>
    public IList<string> AuthorizationServers { get; } = [];
}
