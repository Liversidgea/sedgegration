namespace Sedgegration.Models;

/// <summary>
/// Persisted definition of a workflow — a named, ordered sequence of steps bound to a protocol.
/// </summary>
public class WorkflowDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public List<StepDefinition> Steps { get; set; } = new();
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A single step within a workflow definition.
/// </summary>
public class StepDefinition
{
    public string StepType { get; set; } = string.Empty;
    public int Order { get; set; }
    public Dictionary<string, object> Config { get; set; } = new();
}
