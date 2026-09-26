using System.Net;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>
/// Which redirect URIs an MCP client may register. Any client can connect; only URIs that are unsafe for OAuth are
/// refused (OAuth 2.1, RFC 8252):
/// <list type="bullet">
/// <item><c>https://</c> to any host (Claude, ChatGPT, web clients);</item>
/// <item><c>http://</c> only on a loopback host (<c>localhost</c>, <c>127.0.0.1</c>, <c>[::1]</c>): desktop and CLI
/// clients, MCP Inspector. Codes never travel in clear text over a network;</item>
/// <item>app schemes such as <c>cursor://</c> or <c>vscode://</c> (native apps), except schemes a browser would run or
/// read locally (<c>javascript:</c>, <c>data:</c>, <c>file:</c>, ...).</item>
/// </list>
/// No user info and no fragment. Protection against a malicious client comes from the consent page, which shows the
/// exact redirect URI before anything is sent there, per client.
/// </summary>
public sealed class RedirectUriPolicy
{
    private static readonly HashSet<string> BlockedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "javascript", "data", "file", "vbscript", "about", "blob", "filesystem", "view-source", "ws", "wss", "ftp",
    };

    public bool IsAllowed(string candidate)
    {
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Fragment)
            || candidate.Contains('#'))
        {
            return false;
        }

        return uri.Scheme.ToLowerInvariant() switch
        {
            "https" => !string.IsNullOrEmpty(uri.Host),
            "http" => IsLoopback(uri.Host),
            var scheme => !BlockedSchemes.Contains(scheme),
        };
    }

    /// <summary>Why a URI was refused, for the registration error.</summary>
    public const string Rules = "Redirect URIs must be https, http on localhost / 127.0.0.1 / [::1], or an app scheme (e.g. cursor://).";

    private static bool IsLoopback(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || (IPAddress.TryParse(host.Trim('[', ']'), out var address) && IPAddress.IsLoopback(address));
}
