namespace Sedgegration.Web.Models;

public record RequestViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Protocol { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public byte[] RawData { get; init; } = Array.Empty<byte>();
    public Dictionary<string, object> Metadata { get; init; } = new();
    public DateTime ReceivedAt { get; init; }
    public object? Response { get; init; }
}
