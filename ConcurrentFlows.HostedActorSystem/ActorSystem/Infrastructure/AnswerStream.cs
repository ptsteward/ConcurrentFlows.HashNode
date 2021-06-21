using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ConcurrentFlows.HostedActorSystem.ActorSystem.Infrastructure
{
    public class AnswerStream : BackgroundService, IAnswerStream
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ChannelReader<KeyValuePair<Guid, dynamic>> answerReader;

        public AnswerStream(
            IServiceProvider serviceProvider, 
            ChannelReader<KeyValuePair<Guid, dynamic>> answerReader)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.answerReader = answerReader ?? throw new ArgumentNullException(nameof(answerReader));
            QueryResults = new ConcurrentDictionary<Guid, TaskCompletionSource<dynamic>>();
        }

        private ConcurrentDictionary<Guid, TaskCompletionSource<dynamic>> QueryResults { get; }

        public async ValueTask<dynamic> SubmitQuery<TQuery>(TQuery query)
        {
            var writer = serviceProvider.GetRequiredService<ChannelWriter<KeyValuePair<Guid, TQuery>>>();
            var queryId = Guid.NewGuid();
            var resultSource = new TaskCompletionSource<dynamic>();
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
