using System.Text;
using System.Text.Json;
using Sedgegration.Models;

namespace Sedgegration.Pipeline.Steps;

/// <summary>
/// Deserializes raw bytes into a JSON object based on content type.
/// </summary>
public class DeserializeStep : IPayloadStep
{
    public string Name => "Deserialize";

    public Task ExecuteAsync(PayloadContext context, CancellationToken ct)
    {
        var contentType = context.Metadata.GetValueOrDefault("ContentType") as string ?? "application/json";

        try
        {
            if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                var json = Encoding.UTF8.GetString(context.RawData);
                context.Deserialized = JsonSerializer.Deserialize<JsonElement>(json);
            }
            else if (contentType.Contains("xml", StringComparison.OrdinalIgnoreCase))
            {
                // Store as string for XML — consumers can parse with XDocument/XmlDocument
                context.Deserialized = Encoding.UTF8.GetString(context.RawData);
            }
            else
            {
                // Default: treat as UTF-8 text
                context.Deserialized = Encoding.UTF8.GetString(context.RawData);
            }
        }
        catch (JsonException ex)
        {
            context.Errors.Add($"Deserialization failed: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
