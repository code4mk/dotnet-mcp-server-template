using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace DotnetMcpTemplate.Capabilities.Resources;

/// <summary>Validation for values taken from resource URIs (e.g. {id} in users://{id}).</summary>
internal static class ResourceArguments
{
    public static int PositiveInt(string value, string name) =>
        int.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var number) && number > 0
            ? number
            : throw new McpProtocolException($"{name} must be a positive whole number (was '{value}').", McpErrorCode.InvalidParams);
}
