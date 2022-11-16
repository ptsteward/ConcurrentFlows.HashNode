namespace ConcurrentFlows.AsyncMediator2;

public interface IChannelSource<TPayload>
    where TPayload : notnull
{
    ValueTask<bool> SendAsync(Envelope<TPayload> envelope, CancellationToken cancelToken = default);
}
