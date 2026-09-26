using StackExchange.Redis;

namespace DotnetMcpTemplate.Core.Redis;

/// <summary>
/// Turns REDIS_URL into <see cref="ConfigurationOptions"/>. Custom factories can start from it and add what their
/// vendor needs: <c>var options = RedisConnectionString.Parse(settings.Url);</c>
/// </summary>
public static class RedisConnectionString
{
    /// <summary>
    /// Accepts a StackExchange.Redis connection string or a <c>redis://</c> / <c>rediss://</c> URL. Unless the value says
    /// otherwise, the connection doesn't fail at startup while Redis is unreachable; it keeps retrying in the background.
    /// </summary>
    public static ConfigurationOptions Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();

        if (value.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) || value.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
        {
            return FromUrl(new Uri(value));
        }

        var options = ConfigurationOptions.Parse(value);
        if (!value.Contains("abortConnect", StringComparison.OrdinalIgnoreCase))
        {
            options.AbortOnConnectFail = false;
        }

        return options;
    }

    private static ConfigurationOptions FromUrl(Uri url)
    {
        var tls = url.Scheme.Equals("rediss", StringComparison.OrdinalIgnoreCase);
        var options = new ConfigurationOptions { AbortOnConnectFail = false, Ssl = tls };
        options.EndPoints.Add(url.Host, url.IsDefaultPort || url.Port <= 0 ? 6379 : url.Port);
        if (tls)
        {
            options.SslHost = url.Host;
        }

        // redis://password@host, redis://:password@host, redis://user:password@host (ACL user)
        if (!string.IsNullOrEmpty(url.UserInfo))
        {
            var separator = url.UserInfo.IndexOf(':');
            var user = separator < 0 ? string.Empty : Uri.UnescapeDataString(url.UserInfo[..separator]);
            var password = Uri.UnescapeDataString(separator < 0 ? url.UserInfo : url.UserInfo[(separator + 1)..]);
            if (user.Length > 0)
            {
                options.User = user;
            }

            if (password.Length > 0)
            {
                options.Password = password;
            }
        }

        if (int.TryParse(url.AbsolutePath.Trim('/'), out var database))
        {
            options.DefaultDatabase = database;
        }

        return options;
    }
}
