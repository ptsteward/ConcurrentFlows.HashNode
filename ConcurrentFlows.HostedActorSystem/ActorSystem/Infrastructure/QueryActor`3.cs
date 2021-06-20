using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ConcurrentFlows.HostedActorSystem.ActorSystem.Infrastructure
{
    public abstract class QueryActor<TQuery, TPayload, TAnswer>
        : BackgroundService
        where TQuery : ActorQuery<TPayload, TAnswer>
    {
        protected readonly ChannelReader<KeyValuePair<Guid, TQuery>> queryReader;
        protected readonly ChannelWriter<KeyValuePair<Guid, TAnswer>> answerWriter;

        public QueryActor(
            ChannelReader<KeyValuePair<Guid, TQuery>> queryReader, 
            ChannelWriter<KeyValuePair<Guid, TAnswer>> answerWriter)
        {
            this.queryReader = queryReader ?? throw new ArgumentNullException(nameof(queryReader));
            this.answerWriter = answerWriter ?? throw new ArgumentNullException(nameof(answerWriter));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested && !queryReader.Completion.IsCompleted)
            {
                var messageReady = await queryReader.WaitToReadAsync(stoppingToken);
                if (messageReady)
                {
                    await foreach (var query in queryReader.ReadAllAsync(stoppingToken))
                    {
                        await HandleAsync(query, stoppingToken);
                    }
                }
            }
        }

        public abstract Task HandleAsync(KeyValuePair<Guid, TQuery> query, CancellationToken stoppingToken);
    }
}
