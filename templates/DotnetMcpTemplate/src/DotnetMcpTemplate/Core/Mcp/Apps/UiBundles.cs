using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using DotnetMcpTemplate.Core.Common.Settings;
using ModelContextProtocol;

namespace DotnetMcpTemplate.Core.Mcp.Apps;

/// <summary>
/// Serves built MCP App HTML from ui_dist/. The Vite build in ui/ writes one self-contained HTML file per entry;
/// each is exposed as a <c>ui://</c> resource (see Resources/UiResources.cs) that MCP-Apps-aware clients render
/// in a sandboxed iframe. Bundles are cached, except in dev so rebuilt bundles show up immediately.
/// </summary>
public sealed partial class UiBundles(McpAppsSettings settings, AppSettings app, IHostEnvironment host)
{
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.Ordinal);

    /// <summary>
    /// MCP_UI_DIST_DIR, else ui_dist in the content root: the project folder under dotnet run / watch, so bundles
    /// rebuilt by <c>pnpm run watch</c> show up without a restart; the app folder when published.
    /// </summary>
    public string DistDir => !string.IsNullOrWhiteSpace(settings.DistDir)
        ? Path.GetFullPath(settings.DistDir)
        : new[] { host.ContentRootPath, AppContext.BaseDirectory }
            .Select(root => Path.Combine(root, "ui_dist"))
            .FirstOrDefault(Directory.Exists) ?? Path.Combine(AppContext.BaseDirectory, "ui_dist");

    /// <summary>Reads ui_dist/&lt;entry&gt;.html. Throws a helpful error when the bundle hasn't been built.</summary>
    public string Read(string entry)
    {
        if (!EntryName().IsMatch(entry))
        {
            throw new McpException($"Invalid UI entry name '{entry}'.");
        }

        return app.IsDev ? Load(entry) : _cache.GetOrAdd(entry, Load);
    }

    private string Load(string entry)
    {
        var path = Path.Combine(DistDir, entry + ".html");
        if (!File.Exists(path))
        {
            throw new McpException($"UI bundle not found at {path}. Run `pnpm install && pnpm run build` in ui/.");
        }

        return File.ReadAllText(path);
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,63}$")]
    private static partial Regex EntryName();
}
