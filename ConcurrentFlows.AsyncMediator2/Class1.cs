using System.Collections.Concurrent;
using System.Threading.Channels;

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

    public EnvelopeOutbox(
        Envelope<TPayload> originator,
        Action<Guid> destructor)
    {
        this.originator = originator;
        this.destructor = destructor;
    }

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
    private bool disposed = false;

    public ChannelSink(
        ChannelReader<Envelope<TPayload>> reader,
        IDisposable sinkLink)
    {
        this.reader = reader;
        this.sinkLink = sinkLink;
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
        if (!disposed && disposing)
            sinkLink.Dispose();
        disposed = true;
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

    public async ValueTask<bool> SendAsync(
        Envelope<TPayload> envelope,
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
        var results = await Task.WhenAll(writing);

        return results.All(s => s);
    }

    public SinkFactory<TPayload> SinkFactory
        => () =>
        {
            var linkId = Guid.NewGuid();
            var sinkLink = new SinkLink(linkId, id => channels.Remove(id, out _));
            var channel = Channel.CreateUnbounded<Envelope<TPayload>>();
            channels.TryAdd(linkId, channel);
            var channelSink = new ChannelSink<TPayload>(channel, sinkLink);
            return channelSink;
        };
}

public static class Registrations
{
    public static IServiceCollection AddMsgChannel<TPayload>(this IServiceCollection services)
        where TPayload : notnull
        => services
            .SetSingleton<OutboxFactory<TPayload>>(_
            => (originator, destructor)
                => new EnvelopeOutbox<TPayload>(originator, destructor))
            .SetSingleton<SinkFactory<TPayload>>(sp =>
            {
                var source = sp.GetRequiredService<IChannelSource<TPayload>>();
                var linkable = source as ILinkableSource<TPayload>
                    ?? throw new ArgumentException($"Source {source.GetType().Name} must be linkable", nameof(source));
                return linkable.SinkFactory;
            })
            .SetSingleton<IChannelSource<TPayload>, ChannelSource<TPayload>>()
            .AddTransient<IChannelSink<TPayload>>(sp => sp.GetRequiredService<SinkFactory<TPayload>>().Invoke());
}

public sealed record Message(int Id);

public sealed class Originator
{
    private readonly IChannelSource<Message> source;

    public Originator(IChannelSource<Message> source)
        => this.source = source;

    public async Task ProduceManyAsync(int count, CancellationToken cancelToken)
    {
        var messages = Enumerable.Range(0, count)
            .Select(i => new Message(i).ToEnvelope());
        var sending = messages.Select(async msg => await source.SendAsync(msg));
        await Task.WhenAll(sending);
    }
}

public sealed class Consumer
{
    private readonly IChannelSink<Message> sink;

    public Consumer(IChannelSink<Message> sink)
        => this.sink = sink;

    public async Task<IEnumerable<Message>> CollectAllAsync(CancellationToken cancelToken)
    {
        var set = new List<Message>();
        try
        {

            await foreach (var envelope in sink.ConsumeAsync(cancelToken))
                set.Add(envelope.Payload);
        }
        catch (OperationCanceledException)
        { /*We're Done*/ }
        return set;
    }
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
