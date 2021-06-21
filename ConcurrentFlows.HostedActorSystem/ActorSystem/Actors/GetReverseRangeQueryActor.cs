using ConcurrentFlows.HostedActorSystem.ActorSystem.Infrastructure;
using ConcurrentFlows.HostedActorSystem.Queries.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ConcurrentFlows.HostedActorSystem.ActorSystem.Actors
{
    public class GetReverseRangeQueryActor : QueryActor<GetReverseRangeActorQuery>
    {
        public GetReverseRangeQueryActor(
            ChannelReader<KeyValuePair<Guid, GetReverseRangeActorQuery>> queryReader,
            ChannelWriter<KeyValuePair<Guid, dynamic>> answerWriter)
            : base(queryReader, answerWriter)
        {

        }

        public override async Task HandleAsync(KeyValuePair<Guid, GetReverseRangeActorQuery> query, CancellationToken stoppingToken)
        {
            var range = Enumerable.Range(1, query.Value.Payload).Reverse().ToAsyncEnumerable();
            var answer = new KeyValuePair<Guid, dynamic>(query.Key, range);
            await answerWriter.WriteAsync(answer);
        }
    }
}
