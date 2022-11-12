namespace ConcurrentFlows.AsyncMediator2;

public interface IFlowSink<out TSchema>
    : IAsyncDisposable, IDisposable
    where TSchema : Envelope
{
    IAsyncEnumerable<TSchema> ConsumeAsync(CancellationToken cancelToken = default);
}
