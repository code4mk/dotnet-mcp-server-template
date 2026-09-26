using System.Reflection;
using ModelContextProtocol.Server;

namespace DotnetMcpTemplate.Core.Mcp;

/// <summary>
/// Maps MCP names to the C# methods that implement them. The SDK's primitives don't expose their method,
/// and the validation filter needs the parameters, so this scans the same classes the SDK registers.
/// Every tool and prompt must declare an explicit Name (enforced here and by the architecture tests).
/// </summary>
public sealed class McpHandlerRegistry
{
    public McpHandlerRegistry()
    {
        foreach (var type in typeof(McpHandlerRegistry).Assembly.GetTypes())
        {
            if (type.GetCustomAttribute<McpServerToolTypeAttribute>() is not null)
            {
                Add(Tools, type, m => m.GetCustomAttribute<McpServerToolAttribute>()?.Name, "tool", m => m.IsDefined(typeof(McpServerToolAttribute)));
            }

            if (type.GetCustomAttribute<McpServerPromptTypeAttribute>() is not null)
            {
                Add(Prompts, type, m => m.GetCustomAttribute<McpServerPromptAttribute>()?.Name, "prompt", m => m.IsDefined(typeof(McpServerPromptAttribute)));
            }
        }
    }

    public Dictionary<string, MethodInfo> Tools { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, MethodInfo> Prompts { get; } = new(StringComparer.Ordinal);

    private static void Add(
        Dictionary<string, MethodInfo> target,
        Type type,
        Func<MethodInfo, string?> getName,
        string kind,
        Func<MethodInfo, bool> isHandler)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (var method in type.GetMethods(flags).Where(isHandler))
        {
            var name = getName(method);
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException($"{type.Name}.{method.Name}: every MCP {kind} needs an explicit Name.");
            }

            if (!target.TryAdd(name, method))
            {
                throw new InvalidOperationException($"Duplicate MCP {kind} name '{name}' ({type.Name}.{method.Name}).");
            }
        }
    }
}
