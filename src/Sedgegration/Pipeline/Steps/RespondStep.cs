using System.Text.Json;
using Sedgegration.Models;

namespace Sedgegration.Pipeline.Steps;

/// <summary>
/// Sets a response value from the processed payload so protocol handlers can return it.
/// Config options:
/// - key: metadata key to set (default: "Response")
/// - useRaw: if true, use raw bytes as UTF8 string when Deserialized is null
/// </summary>
public class RespondStep : IPayloadStep
{
    private readonly string _key;
    private readonly bool _useRaw;

    public string Name => "Respond";

    public RespondStep(Dictionary<string, object> config)
    {
        _key = "Response";
        _useRaw = false;
        if (config.TryGetValue("key", out var k))
            _key = k is JsonElement je ? je.GetString() ?? _key : k.ToString() ?? _key;
        if (config.TryGetValue("useRaw", out var ur))
        {
            if (ur is JsonElement jebi && jebi.ValueKind == JsonValueKind.True)
                _useRaw = true;
            else if (bool.TryParse(ur?.ToString() ?? string.Empty, out var pb))
                _useRaw = pb;
        }
    }

    public Task ExecuteAsync(PayloadContext context, CancellationToken ct)
    {
        object? response = null;

        if (context.Deserialized is not null)
        {
            response = context.Deserialized;
        }
        else if (_useRaw && context.RawData is { Length: > 0 })
        {
            response = System.Text.Encoding.UTF8.GetString(context.RawData);
        }
        else
        {
            // Nothing to respond with
            return Task.CompletedTask;
        }

        context.Metadata[_key] = response!;
        return Task.CompletedTask;
    }
}
