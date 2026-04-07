using System.Threading.Channels;

namespace Sedgegration.Requests;

public interface IRequestQueue
{
    ValueTask EnqueueAsync(PersistedRequest request, CancellationToken ct = default);
    ChannelReader<PersistedRequest> Reader { get; }
}
