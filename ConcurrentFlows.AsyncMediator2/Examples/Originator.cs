namespace ConcurrentFlows.AsyncMediator2.Examples;

public sealed class Originator
{
    private readonly IChannelSource<Message> source;

    public Originator(IChannelSource<Message> source)
        => this.source = source;

    public async Task ProduceManyAsync(int count, CancellationToken cancelToken)
    {
        var messages = Enumerable.Range(0, count)
            .Select(i => new Message(i).ToEnvelope());
        var sending = messages.Select(async msg => await source.SendAsync(msg));
        await Task.WhenAll(sending);
    }
}
