using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Sedgegration.Models;
using Sedgegration.Pipeline;

namespace Sedgegration.Workflows;

/// <summary>
/// Compiles workflow definitions into live pipelines and dispatches incoming payloads
/// to all matching, enabled workflows for a given protocol.
/// </summary>
public class WorkflowEngine(IWorkflowStore store, StepRegistry stepRegistry, ILoggerFactory loggerFactory)
{
    private readonly ConcurrentDictionary<string, PayloadPipeline> _activePipelines = new();

    /// <summary>
    /// Compiles a workflow definition into a live pipeline.
    /// </summary>
    private PayloadPipeline Compile(WorkflowDefinition definition)
    {
        var pipeline = new PayloadPipeline(loggerFactory.CreateLogger<PayloadPipeline>());
        foreach (var stepDef in definition.Steps.OrderBy(s => s.Order))
        {
            var step = stepRegistry.Create(stepDef.StepType, stepDef.Config);
            pipeline.AddStep(step);
        }
        return pipeline;
    }

    /// <summary>
    /// Activates a workflow by compiling it into an in-memory pipeline.
    /// </summary>
    public async Task ActivateWorkflowAsync(string workflowId)
    {
        var def = await store.GetAsync(workflowId)
            ?? throw new KeyNotFoundException($"Workflow {workflowId} not found");
        _activePipelines[workflowId] = Compile(def);
    }

    /// <summary>
    /// Removes an active pipeline from memory.
    /// </summary>
    public void DeactivateWorkflow(string workflowId)
        => _activePipelines.TryRemove(workflowId, out _);

    /// <summary>
    /// Processes incoming data through all enabled workflows for the given protocol.
    /// </summary>
    public async Task<List<PayloadContext>> ProcessAsync(
        string protocol, byte[] rawData, Dictionary<string, object> metadata, CancellationToken ct)
    {
        var workflows = await store.GetByProtocolAsync(protocol);
        var results = new List<PayloadContext>();

        foreach (var wf in workflows.Where(w => w.Enabled))
        {
            // Lazy-compile if not yet active
            if (!_activePipelines.TryGetValue(wf.Id, out var pipeline))
            {
                pipeline = Compile(wf);
                _activePipelines[wf.Id] = pipeline;
            }

            var context = new PayloadContext
            {
                ProtocolSource = protocol,
                RawData = rawData,
            };

            foreach (var kv in metadata)
                context.Metadata[kv.Key] = kv.Value;

            results.Add(await pipeline.ExecuteAsync(context, ct));
        }

        return results;
    }

    /// <summary>
    /// Reloads all workflows from the store, recompiling active pipelines.
    /// </summary>
    public async Task ReloadAllAsync()
    {
        _activePipelines.Clear();
        var all = await store.GetAllAsync();
        foreach (var wf in all.Where(w => w.Enabled))
        {
            _activePipelines[wf.Id] = Compile(wf);
        }
    }
}
