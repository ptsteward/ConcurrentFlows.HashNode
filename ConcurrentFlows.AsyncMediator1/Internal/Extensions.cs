namespace ConcurrentFlows.AsyncMediator1.Internal;

public static partial class Extensions
{
    internal static async ValueTask<TReturn> Attempt<TReturn>(
        this Func<ValueTask<TReturn>> func,
        Func<Exception, ValueTask<TReturn>> onError,
        Func<Exception, bool>? canHandle = default)
    {
        try
        {
            return await func();
        }
        catch (Exception ex)
        when (canHandle?.Invoke(ex) ?? ex.IsKnownException())
        {
            return await onError(ex);
        }
    }

    internal static IAsyncEnumerable<T> Attempt<T>(
        this Func<IAsyncEnumerable<T>> iterator,
        Func<Exception, IAsyncEnumerable<T>> onError,
        Func<Exception, bool>? canHandle = default)
        where T : class
    {
        while (true)
        {
            var shouldHandleEx = SetIsErrorDecision(canHandle);
            var enumerable = iterator()
                .GetAsyncEnumerator()
                .ExposeAsyncMoveNext(onError, shouldHandleEx);
            return enumerable;
        }
    }    
}
