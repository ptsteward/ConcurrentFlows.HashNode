namespace ConcurrentFlows.AsyncMediator2.Internal;


internal interface ILinkableSource<TSchema>
    where TSchema : Envelope
{
    IDisposable LinkTo(ITargetBlock<TSchema> sink);
}
