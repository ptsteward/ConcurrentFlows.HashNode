using System.Collections.Concurrent;
using System.Threading.Channels;
using ConcurrentFlows.AsyncMediator2.MsgChannels.Broadcasting;

namespace ConcurrentFlows.AsyncMediator2.MsgChannels;

internal sealed class ChannelSource<TPayload>
    : IChannelSource<TPayload>,
    ILinkableSource<TPayload>
    where TPayload : notnull
{
    private readonly OutboxFactory<TPayload> factory;
    private readonly ConcurrentDictionary<Guid, ChannelWriter<Envelope<TPayload>>> channels = new();
    private readonly ConcurrentDictionary<Guid, IOutbox<TPayload>> outboundMsgs = new();

    public ChannelSource(OutboxFactory<TPayload> outboxFactory)
        => factory = outboxFactory;

    public async ValueTask<bool> SendAsync(
        Envelope<TPayload> envelope,
        CancellationToken cancelToken = default)
    {
        cancelToken.ThrowIfCancellationRequested();
        var outboxId = Guid.NewGuid();
        var outbox = factory(envelope, (id) => outboundMsgs.Remove(id, out _));
        outboundMsgs.TryAdd(outboxId, outbox);
        var writers = channels.Values;

        var writing = writers.Select(async writer =>
        {
            await Task.Yield();
            var outbound = outbox.GetEnvelope();
            return writer.TryWrite(outbound);
        });
        outbox.Complete();
        var results = await Task.WhenAll(writing);

        return results.All(s => s);
    }

    public SinkFactory<TPayload> SinkFactory
        => () =>
        {
            var linkId = Guid.NewGuid();
            var sinkLink = new SinkLink(linkId, id => channels.Remove(id, out _));
            var channel = Channel.CreateUnbounded<Envelope<TPayload>>();
            channels.TryAdd(linkId, channel);
            var channelSink = new ChannelSink<TPayload>(channel, sinkLink);
            return channelSink;
        };
}
