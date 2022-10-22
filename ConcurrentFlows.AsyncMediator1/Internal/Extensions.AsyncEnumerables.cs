namespace ConcurrentFlows.AsyncMediator1.Internal;

public static partial class Extensions
{
    internal static async IAsyncEnumerable<T> ExposeAsyncMoveNext<T>(
        this IAsyncEnumerator<T> enumerator,
        Func<Exception, IAsyncEnumerable<T>> onError,
        Func<Exception, bool> isError)
        where T : class
    {
        T? message = default!;
        try
        {
            var isMore = await enumerator.MoveNextAsync();
            if (isMore)
                message = enumerator.Current;
            else
                message = null;
        }
        catch (Exception ex) when (isError(ex))
        {
            message = await ex.CatchAsyncFallback(onError);
        }
        if (message is not null)
            yield return message;
        else
            yield break;
    }
}
