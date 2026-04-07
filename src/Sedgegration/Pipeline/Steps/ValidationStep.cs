using System.Text.Json;
using Sedgegration.Models;

namespace Sedgegration.Pipeline.Steps;

/// <summary>
/// Validates the deserialized payload. Checks required fields if configured.
/// </summary>
public class ValidationStep : IPayloadStep
{
    private readonly List<string> _requiredFields;

    public string Name => "Validate";

    public ValidationStep(Dictionary<string, object> config)
    {
        _requiredFields = [];

        if (!config.TryGetValue("requiredFields", out var fieldsObj)) 
            return;

        if (fieldsObj is not JsonElement element || element.ValueKind != JsonValueKind.Array) 
            return;
        
        foreach (var item in element.EnumerateArray())
        {
            if (item.GetString() is { } field)
                _requiredFields.Add(field);
        }
    }

    public Task ExecuteAsync(PayloadContext context, CancellationToken ct)
    {
        switch (context.Deserialized)
        {
            case null:
                context.Errors.Add("Validation failed: payload is null");
                break;
            case JsonElement json when json.ValueKind == JsonValueKind.Object:
            {
                foreach (var field in _requiredFields)
                {
                    if (!json.TryGetProperty(field, out _))
                    {
                        context.Errors.Add($"Validation failed: missing required field '{field}'");
                    }
                }

                break;
            }
        }

        return Task.CompletedTask;
    }
}
