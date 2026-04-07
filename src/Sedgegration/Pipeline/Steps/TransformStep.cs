using Sedgegration.Models;

namespace Sedgegration.Pipeline.Steps;

/// <summary>
/// Enriches the payload context with additional metadata.
/// Extensible — override or configure transformations via config.
/// </summary>
public class TransformStep(Dictionary<string, object> config) : IPayloadStep
{
    public string Name => "Transform";

    public Task ExecuteAsync(PayloadContext context, CancellationToken ct)
    {
        // Add a processing timestamp
        context.Metadata["ProcessedAt"] = DateTime.UtcNow.ToString("O");
        context.Metadata["Protocol"] = context.ProtocolSource;

        // Apply any static metadata from config
        foreach (var kv in config)
        {
            context.Metadata[$"Transform.{kv.Key}"] = kv.Value;
        }

        return Task.CompletedTask;
    }
}
