using System.Collections.Concurrent;

namespace ConcurrentFlows.AsyncMediator2;

public static class Extensions
{
    public static Envelope<TPayload> ToEnvelope<TPayload>(
        this TPayload payload,
        TimeSpan timeout,
        Func<Task> onCompleted,
        Func<Task> onFailure)
        where TPayload : notnull
        => new TaskCompletionSource()
            .CreateEnvelope(payload, timeout, onCompleted, onFailure);

    public static Envelope<TPayload> ToEnvelope<TPayload>(
        this TPayload payload,
        TimeSpan timeout)
        where TPayload : notnull
        => payload.ToEnvelope(timeout, () => Task.CompletedTask, () => Task.CompletedTask);

    public static Envelope<TPayload> ToEnvelope<TPayload>(
        this TPayload payload)
        where TPayload : notnull
        => payload.ToEnvelope(TimeSpan.MaxValue, () => Task.CompletedTask, () => Task.CompletedTask);

    private static Envelope<TPayload> CreateEnvelope<TPayload>(
        this TaskCompletionSource taskSource,
        TPayload payload,
        TimeSpan timeout,
        Func<Task> onCompleted,
        Func<Task> onFailure)
        where TPayload : notnull
        => new Envelope<TPayload>(
            Payload: payload,
            TaskSource: taskSource,
            Execution: taskSource.CreateExecutionMonitor(timeout, onCompleted, onFailure));

    private static Task CreateExecutionMonitor(this TaskCompletionSource source,
        TimeSpan timeout,
        Func<Task> onCompleted,
        Func<Task> onFailure)
    {
        return AsyncExecutionMonitor(source.Task, timeout, onCompleted, onFailure);

        async Task AsyncExecutionMonitor(
            Task completion,
            TimeSpan timeout,
            Func<Task> onCompleted,
            Func<Task> onFailure)
        {
            if (await completion.TryWaitAsync(timeout))
                await onCompleted();
            else
                await onFailure();
        }
    }

    public static async Task<bool> TryWaitAsync(this Task task, TimeSpan timeout)
    {
        await Task.WhenAny(task, Task.Delay(timeout));
        return task.IsCompletedSuccessfully;
    }

    public static bool AllTrue(this IEnumerable<bool> set)
        => !set.Any(x => !x);

    public static void MaybeAdd<T>(this ConcurrentBag<T> set, T? item)
    {
        if (item is not null)
            set.Add(item);
    }
}
