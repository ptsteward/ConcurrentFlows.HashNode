﻿using ConcurrentFlows.HostedActorSystem.ActorSystem.Infrastructure;

namespace ConcurrentFlows.HostedActorSystem.Queries.Actor
{
    public record GetFactorialActorQuery(int Payload) : ActorQuery<int, object>(Payload);
}
