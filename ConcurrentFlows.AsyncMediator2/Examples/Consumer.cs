namespace ConcurrentFlows.AsyncMediator2.Examples;

public sealed class Consumer
{
    private readonly IChannelSink<Message> sink;

    public Consumer(IChannelSink<Message> sink)
        => this.sink = sink;

    public async Task<IEnumerable<Message>> CollectAllAsync(CancellationToken cancelToken)
    {
        var set = new List<Message>();
        try
        {

            await foreach (var envelope in sink.ConsumeAsync(cancelToken))
                set.Add(envelope.Payload);
        }
        catch (OperationCanceledException)
        { /*We're Done*/ }
        return set;
    }
}
