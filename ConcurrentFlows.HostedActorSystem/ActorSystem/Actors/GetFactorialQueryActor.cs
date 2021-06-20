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
    public class GetFactorialQueryActor : QueryActor<GetFactorialActorQuery, int, int>
    {
        private readonly IAnswerStream<IAsyncEnumerable<int>> actorStream;

        public GetFactorialQueryActor(
            ChannelReader<KeyValuePair<Guid, GetFactorialActorQuery>> queryReader,
            ChannelWriter<KeyValuePair<Guid, int>> answerWriter,
            IAnswerStream<IAsyncEnumerable<int>> actorStream)
            : base(queryReader, answerWriter)
        {
            this.actorStream = actorStream ?? throw new ArgumentNullException(nameof(actorStream));
        }

        public override async Task HandleAsync(KeyValuePair<Guid, GetFactorialActorQuery> query, CancellationToken stoppingToken)
        {
            var rangeQuery = new GetReverseRangeActorQuery(query.Value.Payload - 1);
            var result = await actorStream.SubmitQuery<GetReverseRangeActorQuery, int>(rangeQuery);
            var factorial = await result.AggregateAsync(query.Value.Payload, (x, y) => x * y);
            var answer = new KeyValuePair<Guid, int>(query.Key, factorial);
            await answerWriter.WriteAsync(answer);
        }
    }
}
