using System.ComponentModel.DataAnnotations;
using DotnetMcpTemplate.Core.Common.Settings;

namespace DotnetMcpTemplate.Core.Auth;

public static class AuthModes
{
    /// <summary>Every MCP request needs a token; clients start login when they connect.</summary>
    public const string Required = "required";

    /// <summary>Anonymous clients can connect; [Authorize] is checked per tool, resource and prompt.</summary>
    public const string Mixed = "mixed";

    /// <summary>No login: a fixed developer user is signed in. Only allowed when APP_ENV=dev.</summary>
    public const string None = "none";
}

public sealed class AuthSettings : IEnvSettings
{
    /// <summary>Name of the auth provider (see Auth/Providers). Built in: oidc, jwt.</summary>
    [ConfigurationKeyName("AUTH_PROVIDER")]
    [Required]
    public string Provider { get; init; } = "oidc";

    [ConfigurationKeyName("MCP_AUTH_MODE")]
    [Required, AllowedValues(AuthModes.Required, AuthModes.Mixed, AuthModes.None)]
    public string Mode { get; init; } = AuthModes.Required;
}
