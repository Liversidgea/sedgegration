using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Sedgegration.Models;

namespace Sedgegration.Protocols;

/// <summary>
/// Manages the lifecycle of protocol handlers, allowing dynamic add/remove at runtime.
/// </summary>
public class ProtocolManager
{
    private record RegistryEntry(Func<IProtocolHandler> Factory, ProtocolDefinition Definition);

    private readonly ConcurrentDictionary<string, IProtocolHandler> _active = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, RegistryEntry> _registry = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, (int Port, string Route)> _activeEndpoints = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<ProtocolManager> _logger;

    public ProtocolManager(
        IEnumerable<(Func<IProtocolHandler> Factory, ProtocolDefinition Definition)> entries,
        ILogger<ProtocolManager> logger)
    {
        _logger = logger;
        foreach (var (factory, definition) in entries)
        {
            _registry[definition.Name] = new RegistryEntry(factory, definition);
        }
    }

    public IReadOnlyList<ProtocolDefinition> RegisteredProtocols =>
        _registry.Values.Select(e => e.Definition).ToList();

    public IReadOnlyList<string> ActiveProtocols => _active.Keys.ToList();

    public void RegisterProtocol(Func<IProtocolHandler> factory, ProtocolDefinition definition)
    {
        if (!_registry.TryAdd(definition.Name, new RegistryEntry(factory, definition)))
            throw new InvalidOperationException($"Protocol '{definition.Name}' is already registered");

        _logger.LogInformation("Protocol {Protocol} registered", definition.Name);
    }

    public async Task UnregisterProtocolAsync(string name, CancellationToken ct = default)
    {
        if (_active.ContainsKey(name))
            await StopProtocolAsync(name, ct);

        if (!_registry.TryRemove(name, out _))
            throw new ArgumentException($"Protocol '{name}' is not registered");

        _logger.LogInformation("Protocol {Protocol} unregistered", name);
    }

    public async Task StartProtocolAsync(string name, CancellationToken ct = default)
    {
        if (!_registry.TryGetValue(name, out var entry))
            throw new ArgumentException($"Unknown protocol: {name}");

        if (_active.ContainsKey(name))
            throw new InvalidOperationException($"Protocol {name} is already running");

        // Use the registered definition to determine effective port/route
        var type = entry.Definition.Type ?? string.Empty;
        var effectiveRoute = entry.Definition.Route;
        var effectivePort = entry.Definition.Port;
        if (type.Equals("http", StringComparison.OrdinalIgnoreCase))
        {
            // See if any active endpoint is listening on same port+route
            var conflict = _activeEndpoints.Values.Any(e => e.Port == effectivePort &&
                                                            string.Equals(e.Route, effectiveRoute, StringComparison.OrdinalIgnoreCase));
            if (conflict)
                throw new InvalidOperationException($"Another HTTP protocol is already listening on port {effectivePort} route '{effectiveRoute}'");
        }

        var handler = entry.Factory();
        // Construct a ProtocolConfig from the definition defaults
        var effectiveConfig = new ProtocolConfig
        {
            Protocol = name,
            Port = effectivePort,
            Route = effectiveRoute,
            Options = new Dictionary<string, object>()
        };

        // If the registered definition specifies a workflow, pass it to the handler via options
        if (!string.IsNullOrWhiteSpace(entry.Definition.Workflow))
            effectiveConfig.Options["Workflow"] = entry.Definition.Workflow;

        await handler.StartAsync(effectiveConfig, ct);
        _active[name] = handler;
        _activeEndpoints[name] = (effectivePort, effectiveRoute);

        _logger.LogInformation("Protocol {Protocol} started", name);
    }

    public async Task StopProtocolAsync(string name, CancellationToken ct = default)
    {
        if (_active.TryRemove(name, out var handler))
        {
            await handler.StopAsync(ct);
            _activeEndpoints.TryRemove(name, out _);
            _logger.LogInformation("Protocol {Protocol} stopped", name);
        }
    }

    public async Task StopAllAsync(CancellationToken ct = default)
    {
        foreach (var name in _active.Keys.ToList())
        {
            await StopProtocolAsync(name, ct);
        }
    }

    public bool IsRunning(string name) => _active.ContainsKey(name);
}
