using System.Text.Json;
using Sedgegration.Models;

namespace Sedgegration.Workflows;

/// <summary>
/// JSON file-backed implementation of <see cref="IWorkflowStore"/>.
/// Thread-safe via SemaphoreSlim.
/// </summary>
public class JsonWorkflowStore : IWorkflowStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly List<WorkflowDefinition> _workflows = [];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JsonWorkflowStore(string filePath)
    {
        _filePath = filePath;

        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            _workflows = JsonSerializer.Deserialize<List<WorkflowDefinition>>(json, JsonOptions) ?? new();
        }
    }

    public Task<IReadOnlyList<WorkflowDefinition>> GetAllAsync()
        => Task.FromResult<IReadOnlyList<WorkflowDefinition>>(_workflows.AsReadOnly());

    public Task<WorkflowDefinition?> GetAsync(string id)
        => Task.FromResult(_workflows.FirstOrDefault(w => w.Id == id));

    public Task<IReadOnlyList<WorkflowDefinition>> GetByProtocolAsync(string protocol)
        => Task.FromResult<IReadOnlyList<WorkflowDefinition>>(
            _workflows
                .Where(w => w.Protocol.Equals(protocol, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly());

    public async Task SaveAsync(WorkflowDefinition workflow)
    {
        await _lock.WaitAsync();
        try
        {
            var existing = _workflows.FindIndex(w => w.Id == workflow.Id);
            if (existing >= 0)
                _workflows[existing] = workflow;
            else
                _workflows.Add(workflow);

            workflow.UpdatedAt = DateTime.UtcNow;
            await PersistAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteAsync(string id)
    {
        await _lock.WaitAsync();
        try
        {
            _workflows.RemoveAll(w => w.Id == id);
            await PersistAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task PersistAsync()
    {
        var json = JsonSerializer.Serialize(_workflows, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
