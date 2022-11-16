using System.Threading.Channels;

namespace ConcurrentFlows.AsyncMediator2.MsgChannels;

internal sealed class ChannelSink<TPayload>
    : IChannelSink<TPayload>,
    IDisposable
    where TPayload : notnull
{
    private readonly ChannelReader<Envelope<TPayload>> reader;
    private readonly IDisposable sinkLink;
    private bool disposed = false;

    public ChannelSink(
        ChannelReader<Envelope<TPayload>> reader,
        IDisposable sinkLink)
    {
        this.reader = reader;
        this.sinkLink = sinkLink;
    }

    public IAsyncEnumerable<Envelope<TPayload>> ConsumeAsync(CancellationToken cancelToken = default)
        => reader.ReadAllAsync(cancelToken);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposed && disposing)
            sinkLink.Dispose();
        disposed = true;
    }
}
