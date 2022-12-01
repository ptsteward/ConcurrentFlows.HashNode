using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;

namespace ConcurrentFlows.AsyncMediator3;

public abstract record Envelope
{
    public virtual string MessageId 
        => $"{GetHashCode()}";
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
    BufferBlock<(TReply? reply, Exception? failure)> Buffer,
    Task Execution)
    : Envelope
    where TPayload : notnull
{
    public Exception? Failure { get; private set; }

    public async Task Complete()
    {        
        Buffer.Complete();
        await Execution;
    }

    public Task Reply(TReply? reply, Exception? failure)
        => Buffer.SendAsync((reply, failure));

    public IAsyncEnumerator<(TReply? reply, Exception? failure)> GetAsyncEnumerator(
        CancellationToken cancelToken = default)
        => Buffer.ReceiveAllAsync()
            .GetAsyncEnumerator(cancelToken);
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
        ;
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
    
{
    ValueTask SendAsync(
        Envelope<TPayload, TReply> envelope,
        CancellationToken cancelToken = default);
}

public interface IChannelSink<TPayload, TReply>
    where TPayload : notnull
    
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

    public abstract TEnvelope GetEnvelope();

    protected virtual async Task EnvelopeDestructorAsync(Guid id)
    {
        Interlocked.Decrement(ref leaseCount);

        if (!ReadyForClosure) return;

        await CloseOutPool(id);
    }

    protected virtual Task CloseOutPool(Guid id)
    {
        destructor(id);
        return Task.CompletedTask;
    }

    private bool ReadyForClosure
        => leaseCount <= 0 && complete;
}

internal sealed class EnvelopeOutbox<TPayload>
    : EnvelopeOutboxBase<Envelope<TPayload>>
    where TPayload : notnull
{
    public EnvelopeOutbox(Envelope<TPayload> originator, Action<Guid> destructor) 
        : base(originator, destructor) { }

    public override Envelope<TPayload> GetEnvelope()
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

    protected override Task EnvelopeDestructorAsync(Guid id)
    {
        var envelope = pool[id];
        failures.MaybeAdd(envelope?.Failure);
        return base.EnvelopeDestructorAsync(id);
    }

    protected override async Task CloseOutPool(Guid id)
    {
        await Task.WhenAll(pool.Select(async e => await e.Value));

        if (failures.Any())
            originator.Fail(new AggregateException(failures));
        else
            originator.Complete();

        await base.CloseOutPool(id);
    }
}

internal sealed class EnvelopeOutbox<TPayload, TReply>
    : EnvelopeOutboxBase<Envelope<TPayload, TReply>>
    where TPayload : notnull
    
{
    public EnvelopeOutbox(Envelope<TPayload, TReply> originator, Action<Guid> destructor)
        : base(originator, destructor) { }

    public override Envelope<TPayload, TReply> GetEnvelope()
    {
        Interlocked.Increment(ref leaseCount);

        var id = Guid.NewGuid();
        var envelope = originator.Payload.ToEnvelope<TPayload, TReply>(
            TimeSpan.FromSeconds(30),
            () => EnvelopeDestructorAsync(id),
            () => EnvelopeDestructorAsync(id));

        pool[id] = envelope;
        return envelope;
    }

    protected override Task EnvelopeDestructorAsync(Guid id) => base.EnvelopeDestructorAsync(id);

    //protected override async Task CloseOutPool(Guid id)
    //{
    //    await Task.WhenAll(pool.Select(async e => await e.Value));

    //    if (failures.Any())
    //        originator.Fail(new AggregateException(failures));
    //    else
    //        originator.Complete();

    //    await base.CloseOutPool(id);
    //}
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
        => throw new NotImplementedException();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        => await Task.Yield();
}

//public static class FromEnvelopeExtensions
//{
//    private static readonly Expression TimeoutExpr 
//        = Expression.Parameter(typeof(TimeSpan), "timeout");

//    private static readonly Expression OnCompletedExpr
//        = Expression.Parameter(typeof(Func<Task>), "onCompleted");

//    private static readonly Expression OnFailureExpr
//        = Expression.Parameter(typeof(Func<Task>), "onFailure");

//    private static readonly Type[] ArgumentTypes
//        = new[]
//        {
//            typeof(TimeSpan),
//            typeof(Func<Task>),
//            typeof(Func<Task>)
//        };

//    private static MethodInfo PayloadEnvelopeMethod<TEnvelope>(
//        this TEnvelope envelope)
//        where TEnvelope : Envelope
//        => typeof(FromEnvelopeExtensions)
//            .GetMethod("FromEnvelope", 1, 
//            ArgumentTypes.Prepend(typeof(TEnvelope)).ToArray());

//    private static MethodInfo ReplyEnvelopeMethod<TEnvelope>(
//        this TEnvelope envelope)
//        where TEnvelope : Envelope
//        => typeof(FromEnvelopeExtensions)
//            .GetMethod("FromEnvelope", 1,
//            ArgumentTypes.Prepend(typeof(TEnvelope)).ToArray());
//}

public static class Extensions
{
    //public static TEnvelope FromEnvelope<TEnvelope>(
    //    this TEnvelope envelope,
    //    TimeSpan timeout,
    //    Func<Task> onCompleted,
    //    Func<Task> onFailure)
    //    where TEnvelope : Envelope
    //{
    //    var typeArgs = typeof(TEnvelope).GenericTypeArguments;
    //    var payloadType = typeArgs.Count() > 0 
    //        ? Expression.Parameter(typeArgs[0], "TPayload")
    //        : null;
    //    var replyType = typeArgs.Count() > 1
    //        ? Expression.Parameter(typeArgs[1], "TReply")
    //        : null;

    //    var paramTypes = new[]
    //    {
    //        typeof(TEnvelope),
    //        typeof(TimeSpan),
    //        typeof(Func<Task>),
    //        typeof(Func<Task>)
    //    };

    //    var getCommandEnvelope = typeof(Extensions).GetMethod(nameof(FromEnvelope), 1, paramTypes);
    //    //var getQueryEnvelope = typeof(Extensions).GetMethod(nameof(FromEnvelope), 2, paramTypes);
        
    //    var exp1 = Expression.Parameter(typeof(TEnvelope), nameof(envelope));
    //    var exp2 = Expression.Parameter(typeof(TimeSpan), nameof(timeout));
    //    var exp3 = Expression.Parameter(typeof(Func<Task>), nameof(onCompleted));   
    //    var exp4 = Expression.Parameter(typeof(Func<Task>), nameof(onFailure));
    //    var paramExpressions = new[]
    //    {
    //        exp1,
    //        exp2,
    //        exp3,
    //        exp4
    //    };
    //    var cmdArgs = paramExpressions.Prepend(payloadType!);
    //    var queryArgs = paramExpressions.Prepend(replyType!).Prepend(payloadType!);

    //    var exp5 = Expression.Call(null, getCommandEnvelope!, cmdArgs);
    //    var exp7 = Expression.Lambda(exp5, cmdArgs);
    //    var x = exp7.Compile().DynamicInvoke();

    //    //var exp6 = Expression.Call(null, getQueryEnvelope!, queryArgs);
    //    //var exp8 = Expression.Lambda(exp6, cmdArgs);
    //    //var x = exp8.Compile().DynamicInvoke();

    //    return (TEnvelope)x!;

    //    //if (typeArgs.Count() == 1)
    //    //    return Expression.Lambda(exp5, paramExpressions))
    //    //else if (type.IsAssignableTo(OpenQueryType))
    //    //    return envelope.FromEnvelope()
    //    //return type switch
    //    //{
    //    //    type.IsAssignableFrom(typeof(Envelope<>))
    //    //}
    //}

    //public static Envelope<TPayload> FromEnvelope<TPayload>(
    //    this Envelope<TPayload> envelope,
    //    TimeSpan timeout,
    //    Func<Task> onCompleted,
    //    Func<Task> onFailure)
    //    where TPayload : notnull
    //    => envelope
    //        .Payload
    //        .ToEnvelope(timeout, onCompleted, onFailure);

    //public static Envelope<TPayload, TReply> FromEnvelope<TPayload, TReply>(
    //    this Envelope<TPayload> envelope,
    //    TimeSpan timeout,
    //    Func<Task> onCompleted,
    //    Func<Task> onFailure)
    //    where TPayload : notnull
    //    
    //    => envelope
    //        .Payload
    //        .ToEnvelope<TPayload, TReply>(timeout, onCompleted, onFailure);

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
            var failed = await completion.TryWaitAsync(timeout);
            if (failed is null)
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
        => new BufferBlock<(TReply? reply, Exception? failure)>()
            .CreateEnvelope(payload, timeout, onCompleted, onFailure);

    public static Envelope<TPayload, TReply> ToEnvelope<TPayload, TReply>(
        this TPayload payload,
        TimeSpan timeout)
        where TPayload : notnull
        => payload.ToEnvelope<TPayload, TReply>(timeout, () => Task.CompletedTask, () => Task.CompletedTask);

    public static Envelope<TPayload, TReply> ToEnvelope<TPayload, TReply>(
        this TPayload payload)
        where TPayload : notnull
        => payload.ToEnvelope<TPayload, TReply>(TimeSpan.MaxValue, () => Task.CompletedTask, () => Task.CompletedTask);

    private static Envelope<TPayload, TReply> CreateEnvelope<TPayload, TReply>(
        this BufferBlock<(TReply? reply, Exception? failure)> buffer,
        TPayload payload,
        TimeSpan timeout,
        Func<Task> onCompleted,
        Func<Task> onFailure)
        where TPayload : notnull
        
        => new Envelope<TPayload, TReply>(
            Payload: payload,
            Buffer: buffer,
            Execution: buffer.CreateExecutionMonitor(timeout, onCompleted, onFailure));

    private static Task CreateExecutionMonitor<TReply>(
        this BufferBlock<(TReply? reply, Exception? failure)> buffer,
        TimeSpan timeout,
        Func<Task> onCompleted,
        Func<Task> onFailure)
        
    {
        return AsyncExecutionMonitor(buffer.Completion, timeout, onCompleted, onFailure);

        async Task AsyncExecutionMonitor(
            Task completion,
            TimeSpan timeout,
            Func<Task> onCompleted,
            Func<Task> onFailure)
        {
            var failed = await completion.TryWaitAsync(timeout);
            if (failed is null)
                await onCompleted();
            else
                await onFailure();
            buffer.Complete();
        }
    }


    public static async Task<Exception?> TryWaitAsync(this Task task, TimeSpan timeout)
    {
        await Task.WhenAny(task.WaitAsync(timeout));
        return task.IsCompletedSuccessfully switch
        {
            true => null,
            false => task.Exception is not null
                ? task.Exception.GetBaseException()
                : new TimeoutException()
        };
    }

    public static async Task<(TReply? reply, Exception? failure)> TryWaitAsync<TReply>(this Task<TReply> task, TimeSpan timeout)
    {
        await Task.WhenAny(task.WaitAsync(timeout));
        return task.IsCompletedSuccessfully switch
        {
            true => (reply: await task, failure: null),
            false => (reply: default, failure: new TimeoutException())
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
