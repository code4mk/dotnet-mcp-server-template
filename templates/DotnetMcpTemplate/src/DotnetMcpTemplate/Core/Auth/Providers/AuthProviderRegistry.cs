using System.Reflection;

namespace DotnetMcpTemplate.Core.Auth.Providers;

/// <summary>Finds every <see cref="IAuthProvider"/> in the loaded assemblies (this project, class libraries, tests).</summary>
public static class AuthProviderRegistry
{
    public static IReadOnlyDictionary<string, IAuthProvider> Discover()
    {
        var providers = new Dictionary<string, IAuthProvider>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(ReferencesThisProject))
        {
            foreach (var type in SafeGetTypes(assembly))
            {
                if (type is not { IsClass: true, IsAbstract: false } || !typeof(IAuthProvider).IsAssignableFrom(type)
                    || type.GetConstructor(Type.EmptyTypes) is null)
                {
                    continue;
                }

                var provider = (IAuthProvider)Activator.CreateInstance(type)!;
                if (!providers.TryAdd(provider.Name, provider))
                {
                    throw new InvalidOperationException(
                        $"Two auth providers are named '{provider.Name}': {providers[provider.Name].GetType().FullName} and {type.FullName}.");
                }
            }
        }

        return providers;
    }

    public static IAuthProvider Get(string name)
    {
        var providers = Discover();
        return providers.TryGetValue(name, out var provider)
            ? provider
            : throw new InvalidOperationException(
                $"AUTH_PROVIDER '{name}' was not found. Available: {string.Join(", ", providers.Keys.Order())}.");
    }

    private static bool ReferencesThisProject(Assembly assembly)
    {
        var self = typeof(IAuthProvider).Assembly;
        return assembly == self || assembly.GetReferencedAssemblies().Any(a => a.Name == self.GetName().Name);
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(t => t is not null)!;
        }
    }
}
