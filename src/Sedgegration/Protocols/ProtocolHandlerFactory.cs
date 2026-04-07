using Microsoft.Extensions.Logging;
using Sedgegration.Workflows;

namespace Sedgegration.Protocols;

/// <summary>
/// Resolves a protocol type string (e.g. "http", "tcp") to a handler factory function.
/// </summary>
public class ProtocolHandlerFactory(WorkflowEngine engine, ILoggerFactory loggerFactory)
{
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "http",
        "tcp"
    };

    public IReadOnlyList<string> AvailableTypes => SupportedTypes.ToList();

    public Func<IProtocolHandler> Resolve(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "http" => () => new HttpProtocolHandler(engine, loggerFactory.CreateLogger<HttpProtocolHandler>()),
            "tcp" => () => new TcpProtocolHandler(engine, loggerFactory.CreateLogger<TcpProtocolHandler>()),
            _ => throw new ArgumentException($"Unknown protocol type: {type}")
        };
    }
}
