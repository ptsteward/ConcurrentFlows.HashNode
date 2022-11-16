namespace ConcurrentFlows.AsyncMediator2;

public abstract record Envelope
{
    public virtual string EnvelopeId => $"{GetHashCode()}";
}

public sealed record Envelope<TPayload>(
    TPayload Payload,
    TaskCompletionSource TaskSource,
    Task Execution)
    : Envelope
    where TPayload : notnull
{
    private TaskCompletionSource TaskSource { get; } = TaskSource;
    private Task Execution { get; } = Execution;

    public Exception? Failure { get; private set; }

    public void Complete()
        => TaskSource.TrySetResult();

    public void Fail(Exception exception)
    {
        TaskSource.TrySetResult();
        Failure = exception;
    }

    public TaskAwaiter GetAwaiter()
        => Execution.GetAwaiter();
}

public sealed record Envelope<TPayload, TReply>(
    TPayload Payload,
    TaskCompletionSource<TReply?> TaskSource,
    Task<TReply?> Execution)
    : Envelope
    where TPayload : notnull
    where TReply : notnull
{
    private TaskCompletionSource<TReply?> TaskSource { get; } = TaskSource;
    private Task<TReply?> Execution { get; } = Execution;

    public Exception? Failure { get; private set; }

    public void Complete(TReply result)
        => TaskSource.TrySetResult(result);

    public void Fail(Exception exception)
    {
        TaskSource.TrySetException(exception);
        Failure = exception;
    }

    public TaskAwaiter<TReply?> GetAwaiter()
        => Execution.GetAwaiter();
}
