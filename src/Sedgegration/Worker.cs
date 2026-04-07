using Sedgegration.Protocols;
using Sedgegration.Workflows;

namespace Sedgegration;

/// <summary>
/// Background service that initializes protocol handlers and workflow engine on startup,
/// and tears them down on shutdown.
/// </summary>
public class ProtocolService(
    ProtocolManager protocolManager,
    WorkflowEngine workflowEngine,
    ILogger<ProtocolService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Sedgegration service starting...");

        // Load all persisted workflows into the engine
        await workflowEngine.ReloadAllAsync();
        logger.LogInformation("Workflows loaded");

        // Keep running — management API handles dynamic protocol start/stop
        logger.LogInformation("Sedgegration service is ready. Use the management API to configure protocols and workflows.");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Sedgegration service stopping...");
        await protocolManager.StopAllAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
