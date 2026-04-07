using System.ComponentModel.DataAnnotations;

namespace Sedgegration.Web.Models;

public record ProtocolViewModel
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

public record ProtocolListResponse
{
    public List<ProtocolViewModel> Registered { get; init; } = new();
    public List<string> Active { get; init; } = new();
}

public record ProtocolStatusResponse
{
    public string Status { get; init; } = string.Empty;
    public string Protocol { get; init; } = string.Empty;
}

public class ProtocolConfigForm
{
    [Required]
    public string Protocol { get; set; } = string.Empty;

    // Port optional on start; manager will use the registered default if zero
    [Range(0, 65535)]
    public int Port { get; set; }

    public string Route { get; set; } = "/ingest";

    public Dictionary<string, object> Options { get; set; } = new();
}

public record ToggleResponse
{
    public string Id { get; init; } = string.Empty;
    public bool Enabled { get; init; }
}

public record ReloadResponse
{
    public string Status { get; init; } = string.Empty;
}

public class RegisterProtocolForm
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public string Direction { get; set; } = string.Empty;

    [Required]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    public string HttpVerb { get; set; } = string.Empty;

    public string Route { get; set; } = "/ingest";

    [Range(1, 65535)]
    public int Port { get; set; }

    public string Workflow { get; set; } = string.Empty;
}
