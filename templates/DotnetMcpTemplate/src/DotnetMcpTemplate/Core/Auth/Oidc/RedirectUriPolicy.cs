using System.Text.RegularExpressions;

namespace DotnetMcpTemplate.Core.Auth.Oidc;

/// <summary>
/// Which redirect URIs MCP clients may register (AUTH_ALLOWED_REDIRECT_URIS).
/// Patterns: exact URIs, or http(s) URIs with <c>*</c> as the port (<c>http://localhost:*</c>) or in the path
/// (<c>https://app.example.com/callback/*</c>). The host is always matched exactly. Other schemes
/// (<c>cursor://...</c>) may use <c>*</c> anywhere after the scheme.
/// </summary>
public sealed class RedirectUriPolicy(OidcProxySettings settings)
{
    public bool IsAllowed(string candidate)
    {
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        return settings.RedirectUriPatterns.Any(pattern => Matches(pattern, uri, candidate));
    }

    private static bool Matches(string pattern, Uri uri, string candidate)
    {
        if (!pattern.Contains('*'))
        {
            return string.Equals(pattern, candidate, StringComparison.Ordinal);
        }

        var http = Regex.Match(pattern, "^(https?)://([^/:*]+)(?::(\\*|\\d+))?(/.*)?$", RegexOptions.IgnoreCase);
        if (!http.Success)
        {
            // Custom scheme: glob after the scheme, same scheme required.
            var scheme = pattern.Split("://", 2)[0];
            return string.Equals(scheme, uri.Scheme, StringComparison.OrdinalIgnoreCase) && Glob(pattern).IsMatch(candidate);
        }

        if (!string.Equals(http.Groups[1].Value, uri.Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(http.Groups[2].Value, uri.Host, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var port = http.Groups[3].Value;
        if (port.Length > 0 && port != "*" && port != uri.Port.ToString(System.Globalization.CultureInfo.InvariantCulture))
        {
            return false;
        }

        if (port.Length == 0 && !uri.IsDefaultPort)
        {
            return false;
        }

        var path = http.Groups[4].Success ? http.Groups[4].Value : null;
        return path is null || Glob(path).IsMatch(uri.PathAndQuery);
    }

    private static Regex Glob(string pattern) =>
        new("^" + Regex.Escape(pattern).Replace("\\*", ".*", StringComparison.Ordinal) + "$", RegexOptions.CultureInvariant);
}
