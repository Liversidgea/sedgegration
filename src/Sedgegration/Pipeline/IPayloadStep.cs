using Sedgegration.Models;

namespace Sedgegration.Pipeline;

/// <summary>
/// A single processing step in a payload pipeline.
/// </summary>
public interface IPayloadStep
{
    string Name { get; }
    Task ExecuteAsync(PayloadContext context, CancellationToken ct);
}
