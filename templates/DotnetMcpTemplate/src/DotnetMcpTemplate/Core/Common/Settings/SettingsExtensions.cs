using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace DotnetMcpTemplate.Core.Common.Settings;

public static class SettingsExtensions
{
    /// <summary>Every concrete <see cref="IEnvSettings"/> class in this project, ordered by name.</summary>
    public static IReadOnlyList<Type> EnvSettingsTypes { get; } = typeof(SettingsExtensions).Assembly
        .GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IEnvSettings).IsAssignableFrom(type))
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    private static readonly MethodInfo AddEnvSettingsMethod = typeof(SettingsExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(method => method is { Name: nameof(AddEnvSettings), IsGenericMethodDefinition: true });

    /// <summary>Registers every <see cref="IEnvSettings"/> class in the project. All are validated at startup.</summary>
    public static IServiceCollection AddAllEnvSettings(this IServiceCollection services, IConfiguration configuration)
    {
        foreach (var type in EnvSettingsTypes)
        {
            AddEnvSettingsMethod.MakeGenericMethod(type).Invoke(null, [services, configuration]);
        }

        return services;
    }

    /// <summary>
    /// Registers a typed settings class bound from environment variables, like a pydantic <c>BaseSettings</c>:
    /// <c>[ConfigurationKeyName("APP_PORT")]</c> names the variable, property initializers are the defaults and
    /// data annotations (<c>[Required]</c>, <c>[Range]</c>, ...) are checked when the app starts.
    /// Inject it directly or as <c>IOptions&lt;T&gt;</c>. Registering a class twice is harmless.
    /// </summary>
    public static IServiceCollection AddEnvSettings<TSettings>(this IServiceCollection services, IConfiguration configuration)
        where TSettings : class
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(TSettings)))
        {
            return services;
        }

        services.AddOptions<TSettings>()
            .Bind(configuration)
            .ValidateOnStart();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<TSettings>, EnvSettingsValidator<TSettings>>());
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<TSettings>>().Value);

        return services;
    }

    /// <summary>
    /// Reads a settings class before the container is built (for registration decisions). Not validated here;
    /// validation happens at startup through the registered class.
    /// </summary>
    public static TSettings ReadEnvSettings<TSettings>(this IConfiguration configuration)
        where TSettings : class, new() =>
        configuration.Get<TSettings>() ?? new TSettings();

    /// <summary>Validates every settings class now and reports all errors at once. Call right after <c>Build()</c>.</summary>
    public static void ValidateSettings(this WebApplication app) =>
        app.Services.GetRequiredService<IStartupValidator>().Validate();

    /// <summary>Splits a comma-separated setting into trimmed, non-empty values.</summary>
    public static IReadOnlyList<string> SplitList(this string? value) =>
        (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
