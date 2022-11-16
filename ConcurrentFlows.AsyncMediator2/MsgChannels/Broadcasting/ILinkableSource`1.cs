namespace ConcurrentFlows.AsyncMediator2.MsgChannels.Broadcasting;

internal interface ILinkableSource<TPayload>
    where TPayload : notnull
{
    SinkFactory<TPayload> SinkFactory { get; }
}
