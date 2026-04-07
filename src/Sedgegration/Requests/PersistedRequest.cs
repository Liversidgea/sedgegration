namespace Sedgegration.Requests;

public record PersistedRequest
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Protocol { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public byte[] RawData { get; init; } = Array.Empty<byte>();
    public Dictionary<string, object> Metadata { get; init; } = new();
    public object? Response { get; init; }
    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
}
