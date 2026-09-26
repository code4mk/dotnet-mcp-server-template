using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DotnetMcpTemplate.Models.Requests;

/// <summary>Input of create_project, with a cross-field rule in <see cref="Validate"/>.</summary>
public sealed record CreateProjectRequest(
    [property: Required, StringLength(100, MinimumLength = 3), Description("Project name.")]
    string Name,

    [property: StringLength(2000), Description("Optional description.")]
    string? Description,

    [property: Required, AllowedValues("low", "medium", "high"), Description("Priority: low, medium or high.")]
    string Priority,

    [property: Range(1, 365), Description("Planned duration in days (1-365).")]
    int DurationDays,

    [property: Range(1, 10), Description("Id of the user who owns the project (1-10 in the sample API).")]
    int OwnerUserId) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Priority == "high" && DurationDays > 90)
        {
            yield return new ValidationResult("High-priority projects must be 90 days or less.", [nameof(DurationDays)]);
        }
    }
}
