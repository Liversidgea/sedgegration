using Microsoft.Extensions.Logging;
using Sedgegration;
using Sedgegration.Api;
using Sedgegration.Models;
using Sedgegration.Pipeline;
using Sedgegration.Pipeline.Steps;
using Sedgegration.Protocols;
using Sedgegration.Workflows;

var builder = WebApplication.CreateBuilder(args);

// Windows service support
builder.Host.UseWindowsService();

// --- Workflow persistence ---
var dataDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "Sedgegration");

builder.Services.AddSingleton<IWorkflowStore>(
    new JsonWorkflowStore(Path.Combine(dataDir, "workflows.json")));

// --- Step registry ---
builder.Services.AddSingleton(sp =>
{
    var registry = new StepRegistry();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

    registry.Register("Deserialize", _ => new DeserializeStep());
    registry.Register("Validate", cfg => new ValidationStep(cfg));
    registry.Register("Transform", cfg => new TransformStep(cfg));
    registry.Register("Log", _ => new LogStep(loggerFactory.CreateLogger<LogStep>()));
    registry.Register("Route", cfg => new RouteStep(cfg, loggerFactory.CreateLogger<RouteStep>()));
    registry.Register("Respond", cfg => new RespondStep(cfg));

    return registry;
});

// --- Workflow engine ---
builder.Services.AddSingleton<WorkflowEngine>();

// --- Protocol handlers ---
builder.Services.AddSingleton<ProtocolHandlerFactory>();
builder.Services.AddSingleton<ProtocolManager>(sp =>
{
    var factory = sp.GetRequiredService<ProtocolHandlerFactory>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

    var entries = new List<(Func<IProtocolHandler>, ProtocolDefinition)>
    {
        (factory.Resolve("http"), new ProtocolDefinition
        {
            Name = "HTTP", Type = "http", Direction = "Inbound",
            ContentType = "application/json", HttpVerb = "POST", Route = "/ingest", Port = 8080, Workflow = "Echo HTTP"
        }),
        (factory.Resolve("tcp"), new ProtocolDefinition
        {
            Name = "TCP", Type = "tcp", Direction = "Inbound",
            ContentType = "application/octet-stream", HttpVerb = "", Route = "", Port = 0, Workflow = ""
        }),
    };

    return new ProtocolManager(entries, loggerFactory.CreateLogger<ProtocolManager>());
});

// --- Background service ---
builder.Services.AddHostedService<ProtocolService>();

var app = builder.Build();

// Seed a simple HTTP echo workflow: Deserialize -> Respond
using (var scope = app.Services.CreateScope())
{
    var store = scope.ServiceProvider.GetRequiredService<IWorkflowStore>();
    var existing = await store.GetAllAsync();
    if (!existing.Any(w => w.Name == "Echo HTTP"))
    {
        var echo = new WorkflowDefinition
        {
            Name = "Echo HTTP",
            Protocol = "HTTP",
            Enabled = true,
            Steps = new List<StepDefinition>
            {
                new StepDefinition { StepType = "Deserialize", Order = 0, Config = new() },
                new StepDefinition { StepType = "Respond", Order = 1, Config = new() }
            }
        };
        await store.SaveAsync(echo);
    }
}

// --- Management API ---
app.MapManagementEndpoints();

app.Run();
