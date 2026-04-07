namespace Sedgegration.Models;

/// <summary>
/// Describes a registered protocol with its metadata.
/// </summary>
public record ProtocolDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string HttpVerb { get; init; } = string.Empty;
    public string Route { get; init; } = "/ingest";
    public int Port { get; init; }
    public string Workflow { get; init; } = string.Empty;
}
