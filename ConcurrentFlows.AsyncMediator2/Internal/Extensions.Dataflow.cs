using ConcurrentFlows.AsyncMediator2;

namespace ConcurrentFlows.AsyncMediator1.Internal;

public static partial class Extensions
{
    internal static bool IsCompleted(this IDataflowBlock block)
        => block?.Completion.IsCompleted ?? true;

    internal static bool IsNotCompleted(this IDataflowBlock block)
        => !block?.IsCompleted() ?? false;

    internal static Func<ValueTask<bool>> OfferAsync<TSchema>(
        this ITargetBlock<TSchema> block,
        TSchema message,
        TimeSpan timeout,
        CancellationToken cancelToken)
        where TSchema : Envelope
        => async () =>
        {
            var submitted = false;
            while (!submitted && block.IsNotCompleted())
            {
                cancelToken.ThrowIfCancellationRequested();
                submitted = await block.SendAsync(message, cancelToken)
                    .WaitAsync(timeout, cancelToken);
                await Task.Yield();
            }
            return submitted;
        };

    internal static Func<IAsyncEnumerable<TSchema>> EnumerateSource<TSchema>(
        this ISourceBlock<TSchema> source,
        CancellationToken cancelToken)
        => () => Enumeration(source, cancelToken);

    internal static async IAsyncEnumerable<TSchema> Enumeration<TSchema>(
        ISourceBlock<TSchema> source,
        [EnumeratorCancellation] CancellationToken cancelToken)
    {
        while (source.IsNotCompleted() && await source.OutputAvailableAsync(cancelToken))
            yield return await source.ReceiveAsync(cancelToken);
        CloseOutEnumeration(source, cancelToken);
    }

    internal static void CloseOutEnumeration<TSchema>(
        ISourceBlock<TSchema> source,
        CancellationToken cancelToken)
    {
        if (source.IsCompleted())
            throw new InvalidOperationException($"{nameof(IsCompleted)}");
        cancelToken.ThrowIfCancellationRequested();
    }
}
