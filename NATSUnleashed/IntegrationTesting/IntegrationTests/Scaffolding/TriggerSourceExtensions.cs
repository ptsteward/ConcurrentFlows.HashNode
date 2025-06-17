using System.Runtime.CompilerServices;

namespace NATSUnleashed.MyNatsApp.IntegrationTests.Scaffolding;

public static class TriggerSourceExtensions
{
    public static CancellationTokenRegistration RegisterTaskCancelCompletion(
        this TaskCompletionSource taskSource,
        CancellationTokenSource cancelSource)
        => cancelSource.Token.Register(tcs => ((TaskCompletionSource)tcs!).TrySetCanceled(), taskSource);

    public static TaskAwaiter GetAwaiter(
        this TaskCompletionSource taskSource)
        => taskSource.Task.GetAwaiter();

    public static IProgress<int> ToProgressCounter(
        this TaskCompletionSource completion,
        int max)
        => new Progress<int>(count =>
        {
            if (count >= max)
                completion.TrySetResult();
        });
}
