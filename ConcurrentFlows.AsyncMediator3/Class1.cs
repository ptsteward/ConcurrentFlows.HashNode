using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace ConcurrentFlows.AsyncMediator3;

public abstract record Envelope
{
    protected string? _messageId;
    public virtual string MessageId
        => _messageId ??= $"{GetHashCode()}";
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
        ArgumentNullException.ThrowIfNull(exception);
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
    {
        ArgumentNullException.ThrowIfNull(result);
        TaskSource.TrySetResult(result);
    }

    public void Fail(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        TaskSource.TrySetException(exception);
        Failure = exception;
    }

    public TaskAwaiter<TReply?> GetAwaiter()
        => Execution.GetAwaiter();
}

public sealed record Command<TPayload>(
    string MessageId,
    TPayload Payload)
    : Envelope
{
    public override string MessageId { get; } = MessageId;
}

public sealed record Query<TPayload, TReply>(
    string MessageId,
    TPayload Payload)
    : Envelope
{
    public override string MessageId { get; } = MessageId;
}

public interface IAsyncMediator
{
    IAsyncEnumerable<Exception?> ExecuteAsync<TPayload>(
        Command<TPayload> command,
        CancellationToken cancelToken = default)
        where TPayload : notnull;

    IAsyncEnumerable<(TReply? Reply, Exception? Failure)> ExecuteAsync<TPayload, TReply>(
        Query<TPayload, TReply> query,
        CancellationToken cancelToken = default)
        where TPayload : notnull
        where TReply : notnull;
}

internal interface IChannelSource<TPayload>
    where TPayload : notnull
{
    ValueTask SendAsync(
        Envelope<TPayload> envelope,
        CancellationToken cancelToken = default);
}

public interface IChannelSink<TPayload>
    where TPayload : notnull
{
    IAsyncEnumerable<Envelope<TPayload>> ConsumeAsync(
        CancellationToken cancelToken = default);
}

internal interface IChannelSource<TPayload, TReply>
    where TPayload : notnull
    where TReply : notnull
{
    ValueTask SendAsync(
        Envelope<TPayload, TReply> envelope,
        CancellationToken cancelToken = default);
}

public interface IChannelSink<TPayload, TReply>
    where TPayload : notnull
    where TReply : notnull
{
    IAsyncEnumerable<Envelope<TPayload, TReply>> ConsumeAsync(
        CancellationToken cancelToken = default);
}

internal abstract class ChannelSinkBase<TEnvelope>
    : IDisposable
    where TEnvelope : Envelope
{
    private readonly ChannelReader<TEnvelope> reader;
    private readonly IDisposable sinkLink;
    private bool disposed = false;

    public ChannelSinkBase(
        ChannelReader<TEnvelope> reader,
        IDisposable sinkLink)
    {
        this.reader = reader;
        this.sinkLink = sinkLink;
    }

    public IAsyncEnumerable<TEnvelope> ConsumeAsync(CancellationToken cancelToken = default)
        => reader.ReadAllAsync(cancelToken);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposed && disposing)
            sinkLink.Dispose();
        disposed = true;
    }
}

internal sealed class ChannelSink<TPayload>
    : ChannelSinkBase<Envelope<TPayload>>
    where TPayload : notnull
{
    public ChannelSink(
        ChannelReader<Envelope<TPayload>> reader,
        IDisposable sinkLink)
        : base(reader, sinkLink) { }
}

internal sealed class ChannelSink<TPayload, TReply>
    : ChannelSinkBase<Envelope<TPayload, TReply>>
    where TPayload : notnull
    where TReply : notnull
{
    public ChannelSink(
        ChannelReader<Envelope<TPayload, TReply>> reader,
        IDisposable sinkLink)
        : base(reader, sinkLink) { }
}

internal abstract class ChannelSourceBase<TEnvelope>
    where TEnvelope : Envelope
{
    private readonly OutboxFactory<TEnvelope> factory;
    private readonly ConcurrentDictionary<Guid, ChannelWriter<TEnvelope>> channels = new();
    private readonly ConcurrentDictionary<Guid, IOutbox<TEnvelope>> outboundMsgs = new();

    public ChannelSourceBase(OutboxFactory<TEnvelope> outboxFactory)
        => factory = outboxFactory;

    public virtual async ValueTask SendAsync(
        TEnvelope envelope,
        CancellationToken cancelToken = default)
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
        await Task.WhenAll(writing);
    }

    public SinkFactory<TEnvelope> SinkFactory(IServiceProvider provider)
        => () =>
        {
            var linkId = Guid.NewGuid();
            var sinkLink = new SinkLink(linkId, id => channels.Remove(id, out _));
            var channel = Channel.CreateUnbounded<TEnvelope>();
            channels.TryAdd(linkId, channel);
            var channelSink = ActivatorUtilities.CreateInstance<IChannelSink<TEnvelope>>(
                provider,
                channel, sinkLink);
            return channelSink;
        };
}

internal sealed class ChannelSource<TPayload>
    : ChannelSourceBase<Envelope<TPayload>>
    where TPayload : notnull
{
    public ChannelSource(OutboxFactory<Envelope<TPayload>> outboxFactory)
        : base(outboxFactory) { }
}

internal sealed class ChannelSource<TPayload, TReply>
    : ChannelSourceBase<Envelope<TPayload, TReply>>
    where TPayload : notnull
    where TReply : notnull
{
    public ChannelSource(OutboxFactory<Envelope<TPayload, TReply>> outboxFactory)
        : base(outboxFactory) { }
}

internal interface IOutbox<TEnvelope>
    where TEnvelope : Envelope
{
    void Complete();
    TEnvelope GetEnvelope();
}

internal abstract class EnvelopeOutboxBase<TEnvelope>
    : IOutbox<TEnvelope>
    where TEnvelope : Envelope
{
    protected readonly TEnvelope originator;
    protected readonly Action<Guid> destructor;
    protected readonly ConcurrentDictionary<Guid, TEnvelope> pool = new();
    protected readonly ConcurrentBag<Exception> failures = new();
    
    protected volatile int leaseCount = 0;
    protected volatile bool complete = false;

    public EnvelopeOutboxBase(
        TEnvelope originator,
        Action<Guid> destructor)
    {
        this.originator = originator;
        this.destructor = destructor;
    }

    public void Complete()
        => complete = true;

    public abstract TEnvelope GetEnvelope()
    {
        Interlocked.Increment(ref leaseCount);

        var id = Guid.NewGuid();
        var envelope = originator.NewEnvelope(
            originator,
            TimeSpan.FromSeconds(30),
            () => EnvelopeDestructorAsync(id),
            () => EnvelopeDestructorAsync(id));

        pool[id] = envelope;
        return envelope;
    }

    private async Task EnvelopeDestructorAsync(Guid id)
    {
        Interlocked.Decrement(ref leaseCount);

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

internal sealed class EnvelopeOutbox<TPayload>
    : EnvelopeOutboxBase<Envelope<TPayload>>
    where TPayload : notnull
{
    public override Envelope<TPayload> GetEnvelope()
    {
        Interlocked.Increment(ref leaseCount);

        var id = Guid.NewGuid();
        var envelope = originator.NewEnvelope(
            originator,
            TimeSpan.FromSeconds(30),
            () => EnvelopeDestructorAsync(id),
            () => EnvelopeDestructorAsync(id));

        pool[id] = envelope;
        return envelope;
    }
}

internal interface ILinkableSource<TEnvelope>
    where TEnvelope : Envelope
{
    SinkFactory<TEnvelope> SinkFactory { get; }
}

internal record struct SinkLink(Guid LinkId) : IDisposable
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

internal delegate IOutbox<TEnvelope> OutboxFactory<TEnvelope>(
    TEnvelope originator,
    Action<Guid> destructor)
    where TEnvelope : Envelope;

internal delegate IChannelSink<TEnvelope> SinkFactory<TEnvelope>()
    where TEnvelope : Envelope;

internal sealed class AsyncMediator
    : BackgroundService,
    IAsyncMediator
{

    private readonly IServiceProvider provider;

    public IAsyncEnumerable<Exception?> ExecuteAsync<TPayload>(Command<TPayload> command, CancellationToken cancelToken = default) where TPayload : notnull => throw new NotImplementedException();
    public IAsyncEnumerable<(TReply? Reply, Exception? Failure)> ExecuteAsync<TPayload, TReply>(Query<TPayload, TReply> query, CancellationToken cancelToken = default)
        where TPayload : notnull
        where TReply : notnull => throw new NotImplementedException();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        => await Task.Yield();
}

internal static class Extensions
{
    public static Envelope<TPayload> FromEnvelope<TPayload>(
        this Envelope<TPayload> envelope,
        TimeSpan timeout,
        Func<Task> onCompleted,
        Func<Task> onFailure)
        where TPayload : notnull
        => envelope
            .Payload
            .ToEnvelope(timeout, onCompleted, onFailure);

    public static Envelope<TPayload, TReply> FromEnvelope<TPayload, TReply>(
        this Envelope<TPayload> envelope,
        TimeSpan timeout,
        Func<Task> onCompleted,
        Func<Task> onFailure)
        where TPayload : notnull
        where TReply : notnull
        => envelope
            .Payload
            .ToEnvelope<TPayload, TReply>(timeout, onCompleted, onFailure);

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


    public static Envelope<TPayload, TReply> ToEnvelope<TPayload, TReply>(
        this TPayload payload,
        TimeSpan timeout,
        Func<Task> onCompleted,
        Func<Task> onFailure)
        where TPayload : notnull
        where TReply : notnull
        => new TaskCompletionSource<TReply?>()
            .CreateEnvelope(payload, timeout, onCompleted, onFailure);

    public static Envelope<TPayload, TReply> ToEnvelope<TPayload, TReply>(
        this TPayload payload,
        TimeSpan timeout)
        where TPayload : notnull
        where TReply : notnull
        => payload.ToEnvelope<TPayload, TReply>(timeout, () => Task.CompletedTask, () => Task.CompletedTask);

    public static Envelope<TPayload, TReply> ToEnvelope<TPayload, TReply>(
        this TPayload payload)
        where TPayload : notnull
        where TReply : notnull
        => payload.ToEnvelope<TPayload, TReply>(TimeSpan.MaxValue, () => Task.CompletedTask, () => Task.CompletedTask);

    private static Envelope<TPayload, TReply> CreateEnvelope<TPayload, TReply>(
        this TaskCompletionSource<TReply?> taskSource,
        TPayload payload,
        TimeSpan timeout,
        Func<Task> onCompleted,
        Func<Task> onFailure)
        where TPayload : notnull
        where TReply : notnull
        => new Envelope<TPayload, TReply>(
            Payload: payload,
            TaskSource: taskSource,
            Execution: taskSource.CreateExecutionMonitor(timeout, onCompleted, onFailure));

    private static Task<TReply?> CreateExecutionMonitor<TReply>(this TaskCompletionSource<TReply?> source,
        TimeSpan timeout,
        Func<Task> onCompleted,
        Func<Task> onFailure)
        where TReply : notnull
    {
        return AsyncExecutionMonitor(source.Task, timeout, onCompleted, onFailure);

        async Task<TReply?> AsyncExecutionMonitor(
            Task<TReply?> completion,
            TimeSpan timeout,
            Func<Task> onCompleted,
            Func<Task> onFailure)
        {
            var attempt = await completion.TryWaitAsync(timeout);
            if (attempt.success)
                await onCompleted();
            else
                await onFailure();
            return attempt.reply;
        }
    }


    public static async Task<bool> TryWaitAsync(this Task task, TimeSpan timeout)
    {
        await Task.WhenAny(task, Task.Delay(timeout));
        return task.IsCompletedSuccessfully;
    }

    public static async Task<(bool success, TReply? reply)> TryWaitAsync<TReply>(this Task<TReply> task, TimeSpan timeout)
    {
        await Task.WhenAny(task, Task.Delay(timeout));
        return task.IsCompletedSuccessfully switch
        {
            true => (success: true, reply: await task),
            false => (success: false, reply: default)
        };
    }

    public static bool AllTrue(this IEnumerable<bool> set)
        => !set.Any(x => !x);

    public static void MaybeAdd<T>(this ConcurrentBag<T> set, T? item)
    {
        if (item is not null)
            set.Add(item);
    }
}
