using Sedgegration.Models;

namespace Sedgegration.Protocols;

/// <summary>
/// Contract for a communication protocol that can be started and stopped dynamically.
/// </summary>
public interface IProtocolHandler
{
    string Name { get; }
    bool IsRunning { get; }
    Task StartAsync(ProtocolConfig config, CancellationToken ct);
    Task StopAsync(CancellationToken ct);
}
