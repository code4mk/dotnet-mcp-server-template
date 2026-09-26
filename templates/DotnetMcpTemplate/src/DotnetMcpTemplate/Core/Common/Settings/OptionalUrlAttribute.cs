using System.ComponentModel.DataAnnotations;

namespace DotnetMcpTemplate.Core.Common.Settings;

/// <summary>Empty is allowed; otherwise the value must be an absolute http(s) URL.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class OptionalUrlAttribute : ValidationAttribute
{
    public OptionalUrlAttribute() : base("The {0} field must be an absolute http or https URL.")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string text
        || string.IsNullOrWhiteSpace(text)
        || (Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp));
}
