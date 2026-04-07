using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sedgegration.Models;

namespace Sedgegration.Pipeline.Steps;

/// <summary>
/// Logs the payload context for diagnostics / auditing.
/// </summary>
public class LogStep(ILogger<LogStep> logger) : IPayloadStep
{
    public string Name => "Log";

    public Task ExecuteAsync(PayloadContext context, CancellationToken ct)
    {
        logger.LogInformation(
            "Payload from {Protocol} | Size={Size} bytes | Metadata={Metadata} | Valid={IsValid}",
            context.ProtocolSource,
            context.RawData.Length,
            JsonSerializer.Serialize(context.Metadata),
            context.IsValid);

        return Task.CompletedTask;
    }
}
