using ConcurrentFlows.MessageHandling.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ConcurrentFlows.MessageHandling.Channels
{
    public class Messenger<TMessage>
        : IMessenger<TMessage>
        where TMessage : class
    {
        private readonly ChannelWriter<TMessage> writer;
        private readonly ChannelReader<TMessage> reader;

        public Messenger()
        {
            var channel = Channel.CreateUnbounded<TMessage>();
            writer = channel.Writer;
            reader = channel.Reader;
        }

        public ValueTask<bool> WaitToReadAsync(CancellationToken token)
            => reader.WaitToReadAsync(token);

        public IAsyncEnumerable<TMessage> ReadAllAsync(CancellationToken token)
            => reader.ReadAllAsync(token);

        public ValueTask WriteAsync(TMessage message)
            => writer.WriteAsync(message);

        public Task Shutdown()
        {
            writer.Complete();
            return reader.Completion;
        }
    }
}
