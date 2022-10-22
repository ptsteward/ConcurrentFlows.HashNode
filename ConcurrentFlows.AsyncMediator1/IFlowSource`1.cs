namespace ConcurrentFlows.AsyncMediator1;

public interface IFlowSource<in TSchema>
    : IAsyncDisposable, IDisposable
    where TSchema : Envelope
{
    ValueTask<bool> EmitAsync(TSchema message, CancellationToken cancelToken = default);
}
