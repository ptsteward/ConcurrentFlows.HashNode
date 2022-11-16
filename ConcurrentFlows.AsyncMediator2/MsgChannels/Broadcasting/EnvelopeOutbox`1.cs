using System.Collections.Concurrent;

namespace ConcurrentFlows.AsyncMediator2.MsgChannels.Broadcasting;

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
