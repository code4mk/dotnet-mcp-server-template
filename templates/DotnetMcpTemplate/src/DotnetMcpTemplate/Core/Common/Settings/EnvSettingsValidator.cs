using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.Options;

namespace DotnetMcpTemplate.Core.Common.Settings;

/// <summary>
/// Data annotation validation that reports the environment variable name,
/// e.g. "OIDC_CLIENT_ID: The OIDC_CLIENT_ID field is required." instead of the C# property name.
/// </summary>
internal sealed class EnvSettingsValidator<TSettings> : IValidateOptions<TSettings>
    where TSettings : class
{
    public ValidateOptionsResult Validate(string? name, TSettings options)
    {
        var errors = EnvSettingsValidation.Validate(options);
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}

internal static class EnvSettingsValidation
{
    /// <summary>Validates every property that has a <see cref="ConfigurationKeyNameAttribute"/>, plus <see cref="IValidatableObject"/>.</summary>
    public static List<string> Validate(object options, string? prefix = null)
    {
        var errors = new List<string>();

        foreach (var property in options.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var envKey = property.GetCustomAttribute<ConfigurationKeyNameAttribute>()?.Name;
            if (envKey is null)
            {
                continue;
            }

            envKey = prefix + envKey;
            var context = new ValidationContext(options) { MemberName = property.Name, DisplayName = envKey };
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateProperty(property.GetValue(options), context, results))
            {
                errors.AddRange(results.Select(r => $"{envKey}: {r.ErrorMessage}"));
            }
        }

        if (errors.Count == 0 && options is IValidatableObject validatable)
        {
            errors.AddRange(validatable.Validate(new ValidationContext(options))
                .Select(r => r.ErrorMessage ?? "Invalid settings."));
        }

        return errors;
    }
}
