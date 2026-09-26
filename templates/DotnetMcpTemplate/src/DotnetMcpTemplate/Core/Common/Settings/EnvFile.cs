using DotNetEnv;

namespace DotnetMcpTemplate.Core.Common.Settings;

/// <summary>
/// Loads the <c>.env</c> file into environment variables and sets the ASP.NET Core environment from APP_ENV.
/// Variables that are already set (Docker, CI, your shell) always win over the file.
/// </summary>
/// <remarks>
/// Raw values can be read anywhere with DotNetEnv, e.g. <c>Env.GetString("APP_URL")</c>.
/// Prefer the typed settings classes for anything the app depends on.
/// </remarks>
public static class EnvFile
{
    private static readonly Dictionary<string, string> AppEnvironments = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dev"] = "Development",
        ["stage"] = "Staging",
        ["prod"] = "Production",
    };

    /// <summary>Call before <c>WebApplication.CreateBuilder</c>.</summary>
    public static void Load()
    {
        // TraversePath: find .env in the current directory or a parent (the repository root).
        // NoClobber: never overwrite variables that are already set.
        Env.TraversePath().NoClobber().Load();

        var appEnv = Env.GetString("APP_ENV");
        if (string.IsNullOrWhiteSpace(appEnv))
        {
            return;
        }

        if (!AppEnvironments.TryGetValue(appEnv, out var environmentName))
        {
            throw new InvalidOperationException(
                $"APP_ENV must be one of: {string.Join(", ", AppEnvironments.Keys)} (was '{appEnv}').");
        }

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environmentName);
    }
}
