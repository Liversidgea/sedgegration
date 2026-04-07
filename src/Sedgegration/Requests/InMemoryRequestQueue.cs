using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Sedgegration.Requests;

public class InMemoryRequestQueue : IRequestQueue, IDisposable
{
    private readonly Channel<PersistedRequest> _channel;

    public InMemoryRequestQueue(int capacity = 1000)
    {
        // Bounded channel with single-writer single-reader semantics by default
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<PersistedRequest>(options);
    }

    public ValueTask EnqueueAsync(PersistedRequest request, CancellationToken ct = default)
    {
        return _channel.Writer.WriteAsync(request, ct);
    }

    public ChannelReader<PersistedRequest> Reader => _channel.Reader;

    public void Dispose()
    {
        _channel.Writer.TryComplete();
    }
}
