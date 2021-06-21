using ConcurrentFlows.HostedActorSystem.ActorSystem.Infrastructure;

namespace ConcurrentFlows.HostedActorSystem.Queries.Actor
{
    public record GetMessageActorQuery(string Payload) : ActorQuery<string, string>(Payload);
}
