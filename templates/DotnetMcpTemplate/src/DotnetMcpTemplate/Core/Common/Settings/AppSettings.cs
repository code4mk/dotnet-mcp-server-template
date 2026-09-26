using System.ComponentModel.DataAnnotations;

namespace DotnetMcpTemplate.Core.Common.Settings;

/// <summary>Project-wide settings from the environment (.env).</summary>
public sealed class AppSettings : IEnvSettings
{
    /// <summary>dev, stage or prod. Also sets the ASP.NET Core environment (see <see cref="EnvFile"/>).</summary>
    [ConfigurationKeyName("APP_ENV")]
    [Required, AllowedValues("dev", "stage", "prod")]
    public string Env { get; init; } = "dev";

    /// <summary>Port the server listens on.</summary>
    [ConfigurationKeyName("APP_PORT")]
    [Range(1, 65535)]
    public int Port { get; init; } = 5080;

    /// <summary>Public base URL clients use, e.g. https://mcp.example.com. Empty = http://localhost:{APP_PORT}.</summary>
    [ConfigurationKeyName("APP_URL")]
    [OptionalUrl]
    public string? Url { get; init; }

    /// <summary>Public base URL without a trailing slash.</summary>
    public string PublicUrl => (string.IsNullOrWhiteSpace(Url) ? $"http://localhost:{Port}" : Url.Trim()).TrimEnd('/');

    public bool IsDev => Env == "dev";

    public bool IsStage => Env == "stage";

    public bool IsProd => Env == "prod";
}
