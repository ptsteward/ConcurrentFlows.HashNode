namespace ConcurrentFlows.AsyncMediator2.MsgChannels.Broadcasting;

internal interface IOutbox<TPayload> where TPayload : notnull
{
    void Complete();
    Envelope<TPayload> GetEnvelope();
}
