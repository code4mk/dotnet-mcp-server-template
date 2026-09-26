namespace DotnetMcpTemplate.Core.Common.Settings;

public static class HostingExtensions
{
    /// <summary>
    /// Listens on APP_PORT on all interfaces, IPv4 and IPv6 (dual-stack), so localhost, 127.0.0.1, [::1] and
    /// 0.0.0.0 all work. The port is set in one place: .env.
    /// </summary>
    public static IWebHostBuilder UseAppPort(this IWebHostBuilder webHost, IConfiguration configuration)
    {
        var raw = configuration["APP_PORT"];
        if (string.IsNullOrWhiteSpace(raw))
        {
            raw = "5080";
        }

        if (!int.TryParse(raw, out var port) || port is < 1 or > 65535)
        {
            throw new InvalidOperationException($"APP_PORT must be a number between 1 and 65535 (was '{raw}').");
        }

        return webHost.UseUrls($"http://*:{port}");
    }

    /// <summary>
    /// Logs the public URL (APP_URL) once the server has started. Kestrel's own line shows the bind address,
    /// http://[::]:{port}, which means "all interfaces" and is not a URL to open.
    /// </summary>
    public static WebApplication LogPublicUrl(this WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var url = app.Services.GetRequiredService<AppSettings>().PublicUrl;
            app.Logger.LogInformation("Server running at {Url}", url);
        });
        return app;
    }
}
