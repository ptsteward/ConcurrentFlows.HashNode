using ConcurrentFlows.ProcessManagement.Infrastructure.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrentFlows.ProcessManagement.Infrastructure.Messaging
{
    public static class MessagingExtensions
    {
        public static async IAsyncEnumerable<TMessage> ContinuousWaitAndReadAllAsync<TMessage>(
            this IMessageSystemReader<TMessage> reader,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            while (!reader.Completion.IsCompleted && !token.IsCancellationRequested)
            {
                var readerReady = await reader.MessageReady(token);
                if (readerReady)
                    await foreach (var message in reader.ReadAllAsync(token))
                        yield return message;
            }
        }

        public static async ValueTask RouteMessageByTypeAsync(this IWriterProvider provider, object message)
        {
            var writer = provider.RequestWriter(message);
            await writer.WriteAsync((dynamic)message);
        }

        public static async ValueTask<bool> SendIfEnding(this ProcessPhase phase, IWriterProvider provider, Func<ProcessEndedMessage> endedMessageFatory)
        {
            if (phase == ProcessPhase.Completed || phase == ProcessPhase.Failed)
            {
                await provider.RouteMessageByTypeAsync(endedMessageFatory());
                return true;
            }
            return false;
        }

        public static bool ContainsActivity<TActivity>(this IEnumerable<dynamic> messages)
            where TActivity : ProcessActivity
            => messages.Any(msg => msg.Activity.GetType() == typeof(TActivity));
    }
}
