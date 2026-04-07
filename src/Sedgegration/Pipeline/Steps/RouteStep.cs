using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sedgegration.Models;

namespace Sedgegration.Pipeline.Steps;

/// <summary>
/// Routes the processed payload to a configured destination.
/// This is a base implementation that logs the routing action.
/// Replace with actual routing logic (DB, queue, file, HTTP forward, etc.).
/// </summary>
public class RouteStep : IPayloadStep
{
    private readonly string _destination;
    private readonly ILogger<RouteStep> _logger;

    public string Name => "Route";

    public RouteStep(Dictionary<string, object> config, ILogger<RouteStep> logger)
    {
        _logger = logger;

        if (config.TryGetValue("destination", out var dest))
        {
            _destination = dest is JsonElement el ? el.GetString() ?? "default" : dest.ToString() ?? "default";
        }
        else
        {
            _destination = "default";
        }
    }

    public Task ExecuteAsync(PayloadContext context, CancellationToken ct)
    {
        _logger.LogInformation(
            "Routing payload from {Protocol} to destination '{Destination}' | Size={Size} bytes",
            context.ProtocolSource,
            _destination,
            context.RawData.Length);

        // TODO: Implement actual routing logic per destination type
        // e.g. write to DB, push to message queue, forward via HTTP, write to file, etc.
        context.Metadata["RoutedTo"] = _destination;
        context.Metadata["RoutedAt"] = DateTime.UtcNow.ToString("O");

        return Task.CompletedTask;
    }
}
