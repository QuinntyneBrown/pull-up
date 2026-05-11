using System.Text.Json;

namespace PullUp.Api.Logging;

// Replaces the values of well-known sensitive JSON property names with a fixed
// ***REDACTED*** marker so request-body logs cannot leak credentials or tokens.
// Walks the document tree; non-JSON input is returned unchanged so the caller
// always has something to log.
public static class JsonBodyRedactor
{
    public const string Marker = "***REDACTED***";

    private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "currentPassword",
        "newPassword",
        "token",
        "refreshToken",
        "resetToken",
        "accessToken",
    };

    public static string Redact(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return body;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                WriteRedacted(document.RootElement, writer);
            }
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (JsonException)
        {
            return body;
        }
    }

    private static void WriteRedacted(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    if (SensitiveProperties.Contains(property.Name))
                    {
                        writer.WriteStringValue(Marker);
                    }
                    else
                    {
                        WriteRedacted(property.Value, writer);
                    }
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteRedacted(item, writer);
                }
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
}
