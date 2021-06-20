using ConcurrentFlows.HostedActorSystem.ActorSystem.Infrastructure;
using System.Collections.Generic;

namespace ConcurrentFlows.HostedActorSystem.Queries.Actor
{
    public record GetReverseRangeActorQuery(int Payload) : ActorQuery<int, IAsyncEnumerable<int>>(Payload);
}
