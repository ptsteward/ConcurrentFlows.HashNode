using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

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

delegate IChannelSink<TPayload> SinkFactory<TPayload>()
    where TPayload : notnull;

delegate IOutbox<TPayload> OutboxFactory<TPayload>(
    Envelope<TPayload> originator,
    Action<Guid> destructor)
    where TPayload : notnull;

public interface IOutbox<TPayload> where TPayload : notnull
{
    void Complete();
    Envelope<TPayload> GetEnvelope();
}

internal interface ILinkableSource<TPayload>
    where TPayload : notnull
{
    SinkFactory<TPayload> SinkFactory { get; }
}

public record struct SinkLink(Guid LinkId) : IDisposable
{
    private Action<Guid>? unlink = default;
    private bool disposed = false;

    public SinkLink(Guid linkId, Action<Guid> unlink)
        : this(linkId)
        => this.unlink = unlink;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing && !disposed)
            unlink?.Invoke(LinkId);
        disposed = true;
        unlink = null;
    }
}

internal sealed class EnvelopeOutbox<TPayload> : IOutbox<TPayload>
    where TPayload : notnull
{
    private readonly Envelope<TPayload> originator;
    private readonly Action<Guid> destructor;
    private readonly ConcurrentDictionary<Guid, Envelope<TPayload>> pool = new();
    private readonly ConcurrentBag<Exception> failures = new();

    private volatile int leaseCount = 0;
    private volatile bool complete = false;

    private EnvelopeOutbox(
        Envelope<TPayload> originator,
        Action<Guid> destructor)
    {
        this.originator = originator;
        this.destructor = destructor;
    }

    internal static OutboxFactory<TPayload> CreateOutbox
        => (originator, destructor)
            => new EnvelopeOutbox<TPayload>(originator, destructor);

    public void Complete()
        => complete = true;

    public Envelope<TPayload> GetEnvelope()
    {
        Interlocked.Increment(ref leaseCount);

        var id = Guid.NewGuid();
        var envelope = originator.Payload.ToEnvelope(
            TimeSpan.FromSeconds(30),
            () => EnvelopeDestructorAsync(id),
            () => EnvelopeDestructorAsync(id));

        pool[id] = envelope;
        return envelope;
    }

    private async Task EnvelopeDestructorAsync(Guid id)
    {
        Interlocked.Decrement(ref leaseCount);

        var envelope = pool[id];
        failures.MaybeAdd(envelope?.Failure);

        if (!ReadyForClosure) return;

        await CloseOutPool(id);
    }

    private async Task CloseOutPool(Guid id)
    {
        await Task.WhenAll(pool.Select(async e => await e.Value));

        if (failures.Any())
            originator.Fail(new AggregateException(failures));
        else
            originator.Complete();

        destructor(id);
    }

    private bool ReadyForClosure
        => leaseCount <= 0 && complete;
}

public interface IChannelSource<TPayload>
    where TPayload : notnull
{
    ValueTask<bool> SendAsync(Envelope<TPayload> envelope, CancellationToken cancelToken = default);
}

public interface IChannelSink<TPayload>
    where TPayload : notnull
{
    IAsyncEnumerable<Envelope<TPayload>> ConsumeAsync(CancellationToken cancelToken = default);
}

internal sealed class ChannelSink<TPayload>
    : IChannelSink<TPayload>,
    IDisposable
    where TPayload : notnull
{
    private readonly ChannelReader<Envelope<TPayload>> reader;
    private readonly IDisposable sinkLink;

    public ChannelSink(
        ChannelReader<Envelope<TPayload>> reader,
        IDisposable fiberLink)
    {
        this.reader = reader;
        this.sinkLink = fiberLink;
    }

    public IAsyncEnumerable<Envelope<TPayload>> ConsumeAsync(CancellationToken cancelToken = default)
        => reader.ReadAllAsync(cancelToken);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        sinkLink.Dispose();
        Dispose();
    }
}

internal sealed class ChannelSource<TPayload>
    : IChannelSource<TPayload>,
    ILinkableSource<TPayload>
    where TPayload : notnull
{
    private readonly OutboxFactory<TPayload> factory;
    private readonly ConcurrentDictionary<Guid, ChannelWriter<Envelope<TPayload>>> channels = new();
    private readonly ConcurrentDictionary<Guid, IOutbox<TPayload>> outboundMsgs = new();

    public ChannelSource(OutboxFactory<TPayload> outboxFactory)
        => this.factory = outboxFactory;

    public async ValueTask<bool> SendAsync(Envelope<TPayload> envelope, CancellationToken cancelToken = default)
    {
        cancelToken.ThrowIfCancellationRequested();
        var outboxId = Guid.NewGuid();
        var outbox = factory(envelope, (id) => outboundMsgs.Remove(id, out _));
        outboundMsgs.TryAdd(outboxId, outbox);
        var writers = channels.Values;

        var writing = writers.Select(async writer =>
        {
            await Task.Yield();
            var outbound = outbox.GetEnvelope();
            return writer.TryWrite(outbound);
        });
        outbox.Complete();
        var results = await Task.WhenAll(writing);

        return results.AllTrue();
    }

    public SinkFactory<TPayload> SinkFactory
        => () =>
        {
            var linkId = Guid.NewGuid();
            var fiberLink = new SinkLink(linkId, id => channels.Remove(id, out _));
            var channel = Channel.CreateUnbounded<Envelope<TPayload>>();
            channels.TryAdd(linkId, channel);
            var channelSink = new ChannelSink<TPayload>(channel, fiberLink);
            return channelSink;
        };
}

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
            try
            {
                if (await completion.TryWaitAsync(timeout))
                    await onCompleted();
                else
                    await onFailure();
            }
            catch (Exception ex)
            {

            }
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
