namespace DotnetMcpTemplate.Core.Common.Settings;

/// <summary>
/// Marks a settings class bound from environment variables. Every class implementing it, anywhere in the
/// project, is registered and validated at startup by <c>AddAllEnvSettings()</c>: no registration line needed.
/// </summary>
/// <remarks>
/// Settings that only apply to one auth provider or one API client are not marked; they are registered
/// by that provider/client (see <c>AddEnvSettings&lt;T&gt;</c>), so unused providers don't require values.
/// </remarks>
public interface IEnvSettings;
