namespace ConcurrentFlows.AsyncMediator2.Internal;

internal sealed class FlowSink<TSchema>
    : IFlowSink<TSchema>
    where TSchema : Envelope
{
    private BufferBlock<TSchema>? Buffer { get; set; }

    private readonly IDisposable link;
    private volatile bool isDisposed;


    public FlowSink(ILinkableSource<TSchema> source)
    {
        Buffer = new(new()
        {
            EnsureOrdered = true,
            BoundedCapacity = DataflowBlockOptions.Unbounded
        });
        link = source.LinkTo(Buffer);
    }

    public IAsyncEnumerable<TSchema> ConsumeAsync(CancellationToken cancelToken = default)
        => Buffer.ThrowIfDisposed(isDisposed)
            .EnumerateSource(cancelToken)
            .Attempt(onError: ex =>
            {
                Dispose();
                return AsyncEnumerable.Empty<TSchema>();
            });

    public void Dispose()
    {
        if (isDisposed)
            return;
        DisposeCore();
    }

    public ValueTask DisposeAsync()
    {
        if (isDisposed)
            return ValueTask.CompletedTask;

        DisposeCore();
        return ValueTask.CompletedTask;
    }

    private void DisposeCore()
    {
        isDisposed = true;
        link?.Dispose();
        Buffer = null;
        GC.SuppressFinalize(this);
    }
}
