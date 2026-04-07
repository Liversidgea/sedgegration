namespace Sedgegration.Models;

/// <summary>
/// Represents a unit of work flowing through the processing pipeline.
/// </summary>
public class PayloadContext
{
    public string ProtocolSource { get; set; } = string.Empty;
    public byte[] RawData { get; set; } = Array.Empty<byte>();
    public object? Deserialized { get; set; }
    public Dictionary<string, object> Metadata { get; } = new();
    public List<string> Errors { get; } = new();
    public bool IsValid => Errors.Count == 0;
}
