using System.ComponentModel.DataAnnotations;
using DotnetMcpTemplate.Core.Common.Settings;

namespace DotnetMcpTemplate.Core.Auth.Jwt;

/// <summary>Settings of the jwt provider (registered only when AUTH_PROVIDER=jwt).</summary>
public sealed class JwtProviderSettings
{
    [ConfigurationKeyName("OIDC_DISCOVERY_URL")]
    [Required, Url]
    public string DiscoveryUrl { get; init; } = string.Empty;

    /// <summary>Accepted audiences, comma-separated. Empty = the MCP URL.</summary>
    [ConfigurationKeyName("OIDC_AUDIENCE")]
    public string? Audience { get; init; }

    /// <summary>Issuer URL derived from the discovery URL (the part before /.well-known/).</summary>
    public string Issuer
    {
        get
        {
            var index = DiscoveryUrl.IndexOf("/.well-known/", StringComparison.OrdinalIgnoreCase);
            return index > 0 ? DiscoveryUrl[..index] : DiscoveryUrl;
        }
    }

    public IReadOnlyList<string> Audiences(string resourceUrl) =>
        Audience.SplitList() is { Count: > 0 } list ? list : [resourceUrl, resourceUrl + "/"];
}
