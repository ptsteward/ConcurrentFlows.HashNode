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
    public class GetReverseRangeQueryActor : QueryActor<GetReverseRangeActorQuery, int, IAsyncEnumerable<int>>
    {
        public GetReverseRangeQueryActor(
            ChannelReader<KeyValuePair<Guid, GetReverseRangeActorQuery>> queryReader,
            ChannelWriter<KeyValuePair<Guid, IAsyncEnumerable<int>>> answerWriter)
            : base(queryReader, answerWriter)
        {

        }

        public override async Task HandleAsync(KeyValuePair<Guid, GetReverseRangeActorQuery> query, CancellationToken stoppingToken)
        {
            var range = Enumerable.Range(1, query.Value.Payload).Reverse().ToAsyncEnumerable();
            var answer = new KeyValuePair<Guid, IAsyncEnumerable<int>>(query.Key, range);
            await answerWriter.WriteAsync(answer);
        }
    }
}
