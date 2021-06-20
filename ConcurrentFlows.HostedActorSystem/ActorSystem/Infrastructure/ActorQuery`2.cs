namespace ConcurrentFlows.HostedActorSystem.ActorSystem.Infrastructure
{
    public record ActorQuery<TPayload, TAnswer>(TPayload Payload);
}
