using Microsoft.Extensions.Logging;
using Sedgegration.Models;

namespace Sedgegration.Pipeline;

/// <summary>
/// Executes an ordered sequence of <see cref="IPayloadStep"/>s against a <see cref="PayloadContext"/>.
/// </summary>
public class PayloadPipeline(ILogger<PayloadPipeline> logger)
{
    private readonly List<IPayloadStep> _steps = new();

    public void AddStep(IPayloadStep step)
    {
        _steps.Add(step);
    }

    public async Task<PayloadContext> ExecuteAsync(PayloadContext context, CancellationToken ct)
    {
        foreach (var step in _steps)
        {
            logger.LogDebug("Executing step: {Step}", step.Name);

            try
            {
                await step.ExecuteAsync(context, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Step {Step} failed", step.Name);
                context.Errors.Add($"{step.Name}: {ex.Message}");
            }

            if (!context.IsValid)
            {
                logger.LogWarning("Pipeline halted at {Step}: {Errors}",
                    step.Name, string.Join("; ", context.Errors));
                break;
            }
        }

        return context;
    }
}
