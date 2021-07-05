using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ConcurrentFlows.ProcessManagement.Infrastructure.Messaging
{
    public class MessageSystem<TMessage>
        : IMessageSystemReader<TMessage>,
        IMessageSystemWriter<TMessage>
    {
        private ChannelReader<TMessage> reader;
        private ChannelWriter<TMessage> writer;

        public MessageSystem()
        {
            var channel = Channel.CreateUnbounded<TMessage>();
            reader = channel.Reader;
            writer = channel.Writer;
        }

        public Task Completion => reader.Completion;

        public ValueTask<bool> MessageReady(CancellationToken token = default)
            => reader.WaitToReadAsync(token);

        public IAsyncEnumerable<TMessage> ReadAllAsync(CancellationToken token = default)
            => reader.ReadAllAsync(token);

        public ValueTask WriteAsync(TMessage message, CancellationToken token = default)
            => writer.WriteAsync(message, token);

        public Task Shutdown()
        {
            writer.Complete();
            return reader.Completion;
        }
    }
}
