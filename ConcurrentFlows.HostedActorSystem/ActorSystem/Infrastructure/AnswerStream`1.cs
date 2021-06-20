using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ConcurrentFlows.HostedActorSystem.ActorSystem.Infrastructure
{
    public class AnswerStream<TAnswer> : BackgroundService, IAnswerStream<TAnswer>
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ChannelReader<KeyValuePair<Guid, TAnswer>> answerReader;

        public AnswerStream(
            IServiceProvider serviceProvider, 
            ChannelReader<KeyValuePair<Guid, TAnswer>> answerReader)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.answerReader = answerReader ?? throw new ArgumentNullException(nameof(answerReader));
            QueryResults = new ConcurrentDictionary<Guid, TaskCompletionSource<TAnswer>>();
        }

        private ConcurrentDictionary<Guid, TaskCompletionSource<TAnswer>> QueryResults { get; }

        public async ValueTask<TAnswer> SubmitQuery<TQuery, TPayload>(TQuery query)
            where TQuery : ActorQuery<TPayload, TAnswer>
        {
            var writer = serviceProvider.GetRequiredService<ChannelWriter<KeyValuePair<Guid, TQuery>>>();
            var queryId = Guid.NewGuid();
            var resultSource = new TaskCompletionSource<TAnswer>();
            QueryResults.TryAdd(queryId, resultSource);
            await writer.WriteAsync(new KeyValuePair<Guid, TQuery>(queryId, query));
            return await resultSource.Task;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested && !answerReader.Completion.IsCompleted)
            {
                var resultsReady = await answerReader.WaitToReadAsync(stoppingToken);
                if (resultsReady)
                    await foreach (var result in answerReader.ReadAllAsync(stoppingToken))
                        if (QueryResults.TryRemove(result.Key, out var resultSource))
                            resultSource.TrySetResult(result.Value);
            }
        }
    }
}
