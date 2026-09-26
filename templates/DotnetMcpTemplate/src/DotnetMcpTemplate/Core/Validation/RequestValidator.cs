using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;

namespace DotnetMcpTemplate.Core.Validation;

/// <summary>
/// Validates MCP arguments against the handler method's parameters: data annotations on parameters
/// (<c>[Range] int limit</c>) and on request objects (<c>[property: Required]</c>), nested objects,
/// collections and <see cref="IValidatableObject"/>. Parameters provided by DI are skipped.
/// </summary>
public static class RequestValidator
{
    private const int MaxDepth = 8;

    public static IReadOnlyList<ValidationError> Validate(
        MethodInfo method,
        IDictionary<string, JsonElement>? arguments,
        Func<Type, bool> isInjected,
        JsonSerializerOptions json)
    {
        var errors = new List<ValidationError>();

        foreach (var parameter in method.GetParameters())
        {
            if (parameter.Name is null || isInjected(parameter.ParameterType))
            {
                continue;
            }

            var name = parameter.Name;
            if (arguments is null || !arguments.TryGetValue(name, out var element) || element.ValueKind is JsonValueKind.Undefined)
            {
                if (parameter.GetCustomAttribute<RequiredAttribute>() is not null
                    || (!parameter.HasDefaultValue && !IsNullable(parameter)))
                {
                    errors.Add(new(name, $"{name} is required."));
                }

                continue;
            }

            object? value;
            try
            {
                value = element.Deserialize(parameter.ParameterType, json);
            }
            catch (JsonException)
            {
                errors.Add(new(name, $"{name} has an invalid value (expected {Describe(parameter.ParameterType)})."));
                continue;
            }

            foreach (var attribute in parameter.GetCustomAttributes<ValidationAttribute>())
            {
                var context = new ValidationContext(value ?? new object()) { MemberName = name, DisplayName = name };
                if (attribute.GetValidationResult(value, context) is { } result && result != ValidationResult.Success)
                {
                    errors.Add(new(name, result.ErrorMessage ?? $"{name} is invalid."));
                }
            }

            ValidateObject(value, name, errors, depth: 0);
        }

        return errors;
    }

    public static string Format(string target, IReadOnlyList<ValidationError> errors) =>
        $"Invalid arguments for {target}:\n" + string.Join('\n', errors.Select(e => $"- {e.Path}: {e.Message}"));

    private static void ValidateObject(object? value, string path, List<ValidationError> errors, int depth)
    {
        if (value is null || depth > MaxDepth || IsSimple(value.GetType()))
        {
            return;
        }

        if (value is IEnumerable items and not string)
        {
            var index = 0;
            foreach (var item in items)
            {
                ValidateObject(item, $"{path}[{index++}]", errors, depth + 1);
            }

            return;
        }

        var before = errors.Count;
        foreach (var property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var jsonName = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            var propertyValue = property.GetValue(value);
            var context = new ValidationContext(value) { MemberName = property.Name, DisplayName = jsonName };
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateProperty(propertyValue, context, results))
            {
                errors.AddRange(results.Select(r => new ValidationError($"{path}.{jsonName}", r.ErrorMessage ?? "is invalid.")));
            }

            ValidateObject(propertyValue, $"{path}.{jsonName}", errors, depth + 1);
        }

        // Cross-field rules only run when the fields themselves are valid.
        if (errors.Count == before && value is IValidatableObject validatable)
        {
            foreach (var result in validatable.Validate(new ValidationContext(value)))
            {
                var member = result.MemberNames.FirstOrDefault();
                var memberPath = member is null ? path : $"{path}.{JsonNamingPolicy.CamelCase.ConvertName(member)}";
                errors.Add(new(memberPath, result.ErrorMessage ?? "is invalid."));
            }
        }
    }

    private static bool IsSimple(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime)
            || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(TimeSpan)
            || type == typeof(Guid) || type == typeof(Uri) || type == typeof(JsonElement);
    }

    private static bool IsNullable(ParameterInfo parameter) =>
        !parameter.ParameterType.IsValueType
            ? new NullabilityInfoContext().Create(parameter).ReadState != NullabilityState.NotNull
            : Nullable.GetUnderlyingType(parameter.ParameterType) is not null;

    private static string Describe(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(string) ? "text"
            : type == typeof(bool) ? "true or false"
            : type.IsPrimitive || type == typeof(decimal) ? "a number"
            : type.IsEnum ? string.Join(", ", Enum.GetNames(type))
            : "an object";
    }
}
