using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sedgegration.Models;
using Sedgegration.Pipeline;
using Sedgegration.Protocols;
using Sedgegration.Requests;
using Sedgegration.Workflows;
using Sedgegration.IO;
using Sedgegration.Pipeline.Steps;

namespace Sedgegration.Api;

/// <summary>
/// Maps the management REST API endpoints for workflows and protocols.
/// </summary>
public static class ManagementApi
{
    // DTO for registering steps at runtime
    public record RegisterStepRequest(string Name);

    public static void MapManagementEndpoints(this IEndpointRouteBuilder app)
    {
        MapHealthEndpoint(app);
        MapWorkflowEndpoints(app);
        MapProtocolEndpoints(app);
        MapStepEndpoints(app);
        MapRequestEndpoints(app);
    }

    private static void MapHealthEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }))
            .WithTags("Health");
    }

    private static void MapWorkflowEndpoints(IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/workflows").WithTags("Workflows");

        // List all workflows
        api.MapGet("/", async (IWorkflowStore store) =>
            Results.Ok(await store.GetAllAsync()));

        // Get single workflow
        api.MapGet("/{id}", async (string id, IWorkflowStore store) =>
        {
            var wf = await store.GetAsync(id);
            return wf is null ? Results.NotFound() : Results.Ok(wf);
        });

        // Create workflow
        api.MapPost("/", async (CreateWorkflowRequest req, IWorkflowStore store,
            WorkflowEngine engine, StepRegistry registry) =>
        {
            var unknownSteps = req.Steps
                .Select(s => s.StepType)
                .Where(t => !registry.Contains(t))
                .ToList();

            if (unknownSteps.Count > 0)
                return Results.BadRequest(new { error = $"Unknown steps: {string.Join(", ", unknownSteps)}" });

            var workflow = new WorkflowDefinition
            {
                Name = req.Name,
                Protocol = req.Protocol,
                Steps = req.Steps.Select((s, i) => new StepDefinition
                {
                    StepType = s.StepType,
                    Order = s.Order ?? i,
                    Config = s.Config ?? new()
                }).ToList(),
                Enabled = req.Enabled
            };

            await store.SaveAsync(workflow);

            if (workflow.Enabled)
                await engine.ActivateWorkflowAsync(workflow.Id);

            return Results.Created($"/api/workflows/{workflow.Id}", workflow);
        });

        // Update workflow
        api.MapPut("/{id}", async (string id, UpdateWorkflowRequest req,
            IWorkflowStore store, WorkflowEngine engine) =>
        {
            var workflow = await store.GetAsync(id);
            if (workflow is null) return Results.NotFound();

            if (req.Name is not null) workflow.Name = req.Name;
            if (req.Protocol is not null) workflow.Protocol = req.Protocol;
            if (req.Steps is not null)
            {
                workflow.Steps = req.Steps.Select((s, i) => new StepDefinition
                {
                    StepType = s.StepType,
                    Order = s.Order ?? i,
                    Config = s.Config ?? new()
                }).ToList();
            }
            if (req.Enabled.HasValue) workflow.Enabled = req.Enabled.Value;

            await store.SaveAsync(workflow);

            if (workflow.Enabled)
                await engine.ActivateWorkflowAsync(workflow.Id);
            else
                engine.DeactivateWorkflow(workflow.Id);

            return Results.Ok(workflow);
        });

        // Delete workflow
        api.MapDelete("/{id}", async (string id, IWorkflowStore store, WorkflowEngine engine) =>
        {
            engine.DeactivateWorkflow(id);
            await store.DeleteAsync(id);
            return Results.NoContent();
        });

        // Toggle enable/disable
        api.MapPost("/{id}/toggle", async (string id, IWorkflowStore store, WorkflowEngine engine) =>
        {
            var workflow = await store.GetAsync(id);
            if (workflow is null) return Results.NotFound();

            workflow.Enabled = !workflow.Enabled;
            await store.SaveAsync(workflow);

            if (workflow.Enabled)
                await engine.ActivateWorkflowAsync(workflow.Id);
            else
                engine.DeactivateWorkflow(workflow.Id);

            return Results.Ok(new { workflow.Id, workflow.Enabled });
        });

        // Reload all workflows from store
        api.MapPost("/reload", async (WorkflowEngine engine) =>
        {
            await engine.ReloadAllAsync();
            return Results.Ok(new { status = "reloaded" });
        });
    }

    private static void MapProtocolEndpoints(IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/protocols").WithTags("Protocols");

        // List registered and active protocols
        api.MapGet("/", (ProtocolManager manager) =>
            Results.Ok(new
            {
                registered = manager.RegisteredProtocols,
                active = manager.ActiveProtocols
            }));

        // Start a protocol
        api.MapPost("/{name}/start", async (string name, ProtocolManager manager) =>
        {
            try
            {
                await manager.StartProtocolAsync(name);
                return Results.Ok(new { status = "started", protocol = name });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // Stop a protocol
        api.MapPost("/{name}/stop", async (string name, ProtocolManager manager) =>
        {
            await manager.StopProtocolAsync(name);
            return Results.Ok(new { status = "stopped", protocol = name });
        });

        // Register a new protocol
        api.MapPost("/", (RegisterProtocolRequest req, ProtocolManager manager,
            ProtocolHandlerFactory factory) =>
        {
            try
            {
                var handlerFactory = factory.Resolve(req.Type);
                var definition = new ProtocolDefinition
                {
                    Name = req.Name,
                    Type = req.Type,
                    Direction = req.Direction,
                    ContentType = req.ContentType,
                    HttpVerb = req.HttpVerb,
                    Route = req.Route,
                    Port = req.Port
                };
                manager.RegisterProtocol(handlerFactory, definition);
                return Results.Created($"/api/protocols/{Uri.EscapeDataString(req.Name)}", definition);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        });

        // List available protocol types
        api.MapGet("/types", (ProtocolHandlerFactory factory) =>
            Results.Ok(factory.AvailableTypes));

        // Unregister a protocol
        api.MapDelete("/{name}", async (string name, ProtocolManager manager) =>
        {
            try
            {
                await manager.UnregisterProtocolAsync(name);
                return Results.NoContent();
            }
            catch (ArgumentException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        });
    }

    private static void MapStepEndpoints(IEndpointRouteBuilder app)
    {
        // List available step types
        app.MapGet("/api/steps", (StepRegistry registry) =>
            Results.Ok(registry.GetRegisteredSteps()))
            .WithTags("Steps");

        // Register a step implementation at runtime (supports WriteFile)
        app.MapPost("/api/steps/register", async (RegisterStepRequest req, StepRegistry registry, DirectoryFileWriter writer, ILoggerFactory loggerFactory) =>
        {
            if (string.IsNullOrWhiteSpace(req?.Name))
                return Results.BadRequest(new { error = "Name is required" });

            var name = req.Name;
            if (registry.Contains(name))
                return Results.Conflict(new { error = "Step already registered" });

            if (string.Equals(name, "WriteFile", StringComparison.OrdinalIgnoreCase))
            {
                registry.Register("WriteFile", cfg => new WriteFileStep(cfg, writer, loggerFactory.CreateLogger<WriteFileStep>()));
                return Results.Created($"/api/steps/{Uri.EscapeDataString(name)}", new { name });
            }

            return Results.BadRequest(new { error = "Unknown or unsupported step type" });
        });
    }

    private static void MapRequestEndpoints(IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/requests").WithTags("Requests");

        api.MapGet("/", async (IRequestStore store) =>
            Results.Ok(await store.GetAllAsync()));

        api.MapGet("/{id}", async (string id, IRequestStore store) =>
        {
            var req = await store.GetAsync(id);
            return req is null ? Results.NotFound() : Results.Ok(req);
        });

        api.MapPost("/{id}/replay", async (string id, IRequestStore store, WorkflowEngine engine) =>
        {
            var req = await store.GetAsync(id);
            if (req is null) return Results.NotFound(new { error = "Request not found" });

            // Ensure metadata dictionary exists
            var metadata = req.Metadata ?? new Dictionary<string, object>();
            metadata["ReplayedAt"] = DateTime.UtcNow;

            var results = await engine.ProcessAsync(req.Path ?? string.Empty, req.RawData ?? Array.Empty<byte>(), metadata, CancellationToken.None);

            var anyResponse = results.Any(r => r.Metadata.ContainsKey("Response"));
            return Results.Ok(new { workflows = results.Count, anyResponse });
        });
    }

    // Simple route validation: allow empty (handler default) or a path starting with '/', no spaces, no '..'
    private static bool IsValidRoute(string? route)
    {
        if (string.IsNullOrEmpty(route)) return true;
        if (!route.StartsWith('/')) return false;
        if (route.Contains(' ') || route.Contains("..")) return false;
        return true;
    }
}
