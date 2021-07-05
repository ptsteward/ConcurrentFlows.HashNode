using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrentFlows.ProcessManagement.Infrastructure.Messaging
{
    public interface IMessageSystemReader<out TMessage>
    {
        public ValueTask<bool> MessageReady(CancellationToken token = default);
        public IAsyncEnumerable<TMessage> ReadAllAsync(CancellationToken token = default);
        public Task Completion { get; }
    }
}
