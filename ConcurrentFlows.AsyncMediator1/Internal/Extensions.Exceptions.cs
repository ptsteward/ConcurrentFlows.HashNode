namespace ConcurrentFlows.AsyncMediator1.Internal;

public static partial class Extensions
{
    [return: NotNull]
    internal static T ThrowIfDisposed<T>(
       this T? target,
       bool isDisposed)
       => (target, isDisposed) switch
       {
           (_, true) => throw new ObjectDisposedException(typeof(T).Name),
           (null, _) => throw new ObjectDisposedException(typeof(T).Name),
           (not null, false) => target,
       };

    internal static bool IsKnownException(this Exception ex)
        => ex is TimeoutException ||
            ex is OperationCanceledException ||
            ex is InvalidOperationException;

    internal static async ValueTask<T> CatchAsyncFallback<T>(
        this Exception ex,
        Func<Exception, IAsyncEnumerable<T>> onError)
        where T : class
    {
        T? message;
        var fallback = onError(ex).GetAsyncEnumerator();
        message = await fallback.MoveNextAsync() switch
        {
            true => fallback.Current,
            _ => default!
        };
        return message;
    }

    internal static Func<Exception, bool> SetIsErrorDecision(
        Func<Exception, bool>? isError = default)
        => isError switch
        {
            not null => isError,
            _ => ex => ex.IsKnownException()
        };
}
