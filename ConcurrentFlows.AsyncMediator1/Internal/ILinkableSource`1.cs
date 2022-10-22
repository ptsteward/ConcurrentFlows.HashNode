namespace ConcurrentFlows.AsyncMediator1.Internal;


internal interface ILinkableSource<TSchema>
    where TSchema : Envelope
{
    IDisposable LinkTo(ITargetBlock<TSchema> sink);
}
