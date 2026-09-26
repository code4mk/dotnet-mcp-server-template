using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DotnetMcpTemplate.Models.Requests;

/// <summary>Input of search_users. Attributes describe the JSON schema the model sees AND are validated on every call.</summary>
public sealed record SearchUsersRequest(
    [property: Required, StringLength(100, MinimumLength = 2), Description("Text to find in the user's name, username or email.")]
    string Query,

    [property: Range(1, 1000), Description("Page number, starting at 1.")]
    int Page = 1,

    [property: Range(1, 50), Description("Results per page (1-50).")]
    int PageSize = 10);
