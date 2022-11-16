namespace ConcurrentFlows.AsyncMediator2.MsgChannels.Broadcasting;

internal delegate IOutbox<TPayload> OutboxFactory<TPayload>(
    Envelope<TPayload> originator,
    Action<Guid> destructor)
    where TPayload : notnull;

internal delegate IChannelSink<TPayload> SinkFactory<TPayload>()
    where TPayload : notnull;
