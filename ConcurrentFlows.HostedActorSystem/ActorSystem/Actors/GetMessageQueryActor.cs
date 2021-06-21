using ConcurrentFlows.HostedActorSystem.ActorSystem.Infrastructure;
using ConcurrentFlows.HostedActorSystem.Queries.Actor;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ConcurrentFlows.HostedActorSystem.ActorSystem.Actors
{
    public class GetMessageQueryActor : QueryActor<GetMessageActorQuery>
    {
        public GetMessageQueryActor(
            ChannelReader<KeyValuePair<Guid, GetMessageActorQuery>> queryReader,
            ChannelWriter<KeyValuePair<Guid, dynamic>> answerWriter)
            : base(queryReader, answerWriter)
        {

        }

        public override async Task HandleAsync(KeyValuePair<Guid, GetMessageActorQuery> query, CancellationToken stoppingToken)
        {
            await answerWriter.WriteAsync(new KeyValuePair<Guid, dynamic>(query.Key, query.Value.Payload));
        }
    }
}
