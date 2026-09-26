using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace DotnetMcpTemplate.Capabilities.Resources;

internal static class JsonResource
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static TextResourceContents Create(string uri, object value) => new()
    {
        Uri = uri,
        MimeType = "application/json",
        Text = JsonSerializer.Serialize(value, Json),
    };
}
