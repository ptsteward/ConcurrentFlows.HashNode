namespace ConcurrentFlows.AsyncMediator2;

public interface IChannelSink<TPayload>
    where TPayload : notnull
{
    IAsyncEnumerable<Envelope<TPayload>> ConsumeAsync(CancellationToken cancelToken = default);
}
