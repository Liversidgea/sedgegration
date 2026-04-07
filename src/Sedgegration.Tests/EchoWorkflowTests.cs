﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sedgegration.Models;
using Sedgegration.Workflows;
using Sedgegration.Pipeline;
using Sedgegration.Pipeline.Steps;
using Xunit;

namespace Sedgegration.Tests;

public class EchoWorkflowTests
{
    [Fact]
    public async Task RespondStep_EchoesJsonPayload()
    {
        // Arrange
        var store = new InMemoryWorkflowStore();
        var registry = new StepRegistry();
        registry.Register("Deserialize", _ => new DeserializeStep());
        registry.Register("Respond", cfg => new RespondStep(cfg));
        var loggerFactory = new Microsoft.Extensions.Logging.LoggerFactory();
        var engine = new WorkflowEngine(store, registry, loggerFactory);

        // Create workflow
        var wf = new WorkflowDefinition
        {
            Name = "TestEcho",
            Protocol = "HTTP",
            Enabled = true,
            Steps = new List<StepDefinition>
            {
                new StepDefinition { StepType = "Deserialize", Order = 0, Config = new() },
                new StepDefinition { StepType = "Respond", Order = 1, Config = new() }
            }
        };
        await store.SaveAsync(wf);
        await engine.ActivateWorkflowAsync(wf.Id);

        var payload = JsonSerializer.SerializeToUtf8Bytes(new { message = "hello" });
        var metadata = new Dictionary<string, object> { ["ContentType"] = "application/json" };

        // Act
        var results = await engine.ProcessAsync(wf.Id, payload, metadata, CancellationToken.None);

        // Assert
        Assert.Single(results);
        var ctx = results[0];
        Assert.True(ctx.Metadata.ContainsKey("Response"));
        var resp = ctx.Metadata["Response"];
        Assert.IsType<JsonElement>(resp);
        var je = (JsonElement)resp;
        Assert.Equal("hello", je.GetProperty("message").GetString());
    }
}

// Minimal in-memory IWorkflowStore for tests
internal class InMemoryWorkflowStore : IWorkflowStore
{
    private readonly List<WorkflowDefinition> _list = new();
    public Task<IReadOnlyList<WorkflowDefinition>> GetAllAsync() => Task.FromResult<IReadOnlyList<WorkflowDefinition>>(_list.AsReadOnly());
    public Task<WorkflowDefinition?> GetAsync(string id) => Task.FromResult(_list.FirstOrDefault(w => w.Id == id));
    public Task<IReadOnlyList<WorkflowDefinition>> GetByProtocolAsync(string protocol) => Task.FromResult<IReadOnlyList<WorkflowDefinition>>(_list.Where(w => w.Protocol.Equals(protocol, StringComparison.OrdinalIgnoreCase)).ToList().AsReadOnly());
    public Task<WorkflowDefinition?> GetByNameAsync(string name) => Task.FromResult(_list.FirstOrDefault(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)));
    public Task SaveAsync(WorkflowDefinition workflow)
    {
        var idx = _list.FindIndex(w => w.Id == workflow.Id);
        if (idx >= 0) _list[idx] = workflow; else _list.Add(workflow);
        return Task.CompletedTask;
    }
    public Task DeleteAsync(string id) { _list.RemoveAll(w => w.Id == id); return Task.CompletedTask; }
}
