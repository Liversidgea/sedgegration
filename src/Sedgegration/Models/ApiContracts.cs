namespace Sedgegration.Models;

public record CreateWorkflowRequest
{
    public string Name { get; init; } = string.Empty;
    public string Protocol { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    public List<StepRequest> Steps { get; init; } = new();
}

public record UpdateWorkflowRequest
{
    public string? Name { get; init; }
    public string? Protocol { get; init; }
    public bool? Enabled { get; init; }
    public List<StepRequest>? Steps { get; init; }
}

public record StepRequest
{
    public string StepType { get; init; } = string.Empty;
    public int? Order { get; init; }
    public Dictionary<string, object>? Config { get; init; }
}

public record ProtocolConfig
{
    public string Protocol { get; init; } = string.Empty;
    public int Port { get; init; }
    public string Route { get; init; } = "/ingest";
    public Dictionary<string, object> Options { get; init; } = new();
}

public record RegisterProtocolRequest
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
