using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sedgegration.Models;
using Sedgegration.Workflows;

namespace Sedgegration.Protocols;

public class HttpProtocolHandler(WorkflowEngine engine, ILogger<HttpProtocolHandler> logger) : IProtocolHandler
{
    private WebApplication? _app;
    private string _route = "/ingest";

    public string Name => "HTTP";
    public bool IsRunning => _app is not null;

    public async Task StartAsync(ProtocolConfig config, CancellationToken ct)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://*:{config.Port}");
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        _app = builder.Build();

        // Prefer route from config, fall back to default
        _route = string.IsNullOrWhiteSpace(config.Route) ? "/ingest" : config.Route;

        ConfigureRoutes(_app);

        logger.LogInformation("HTTP listener starting on port {Port} (route: {Route})", config.Port, _route);
        await _app.StartAsync(ct);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        if (_app is not null)
        {
            await _app.StopAsync(ct);
            await _app.DisposeAsync();
            _app = null;
        }
        logger.LogInformation("HTTP listener stopped");
    }

    private void ConfigureRoutes(WebApplication app)
    {
        // Map ingest route from configuration
        app.MapPost(_route, async (HttpContext http, CancellationToken ct) =>
        {
            using var ms = new MemoryStream();
            await http.Request.Body.CopyToAsync(ms, ct);

            var metadata = new Dictionary<string, object>
            {
                ["ContentType"] = http.Request.ContentType ?? "application/json",
                ["Path"] = http.Request.Path.Value ?? "/",
                ["Method"] = http.Request.Method
            };

            // Copy query string parameters
            foreach (var q in http.Request.Query)
            {
                metadata[$"Query.{q.Key}"] = q.Value.ToString();
            }

            // Copy selected headers
            foreach (var h in http.Request.Headers)
            {
                metadata[$"Header.{h.Key}"] = h.Value.ToString();
            }

            var results = await engine.ProcessAsync("HTTP", ms.ToArray(), metadata, ct);

            // If any workflow produced a response, return the first one found
            var responseContext = results.FirstOrDefault(r => r.Metadata.ContainsKey("Response"));
            if (responseContext is not null)
            {
                var resp = responseContext.Metadata["Response"]!;
                return resp switch
                {
                    System.Text.Json.JsonElement je => Results.Json(je),
                    string s => Results.Text(s),
                    _ => Results.Json(resp)
                };
            }

            var allValid = results.TrueForAll(r => r.IsValid);
            return allValid
                ? Results.Ok(new { status = "processed", workflows = results.Count })
                : Results.BadRequest(new
                {
                    status = "error",
                    errors = results.SelectMany(r => r.Errors).ToList()
                });
        });

        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
    }
}
