using Sedgegration.Models;

namespace Sedgegration.Workflows;

/// <summary>
/// Persistence layer for workflow definitions.
/// </summary>
public interface IWorkflowStore
{
    Task<IReadOnlyList<WorkflowDefinition>> GetAllAsync();
    Task<WorkflowDefinition?> GetAsync(string id);
    Task<IReadOnlyList<WorkflowDefinition>> GetByProtocolAsync(string protocol);
    Task SaveAsync(WorkflowDefinition workflow);
    Task DeleteAsync(string id);
}
