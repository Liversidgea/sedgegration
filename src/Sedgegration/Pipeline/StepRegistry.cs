using System.Collections.Concurrent;

namespace Sedgegration.Pipeline;

/// <summary>
/// Registry that maps step type names to factory functions,
/// allowing dynamic creation of pipeline steps from workflow definitions.
/// </summary>
public class StepRegistry
{
    private readonly ConcurrentDictionary<string, Func<Dictionary<string, object>, IPayloadStep>> _factories = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string name, Func<Dictionary<string, object>, IPayloadStep> factory)
        => _factories[name] = factory;

    public IPayloadStep Create(string name, Dictionary<string, object> config)
    {
        return !_factories.TryGetValue(name, out var factory) ? 
            throw new InvalidOperationException($"Unknown step type: {name}") :
            factory(config);
    }

    public IReadOnlyList<string> GetRegisteredSteps() => _factories.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();

    public bool Contains(string name) => _factories.ContainsKey(name);
}
