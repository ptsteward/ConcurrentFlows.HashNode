using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrentFlows.MessageHandling.Interfaces
{
    public interface IMessenger<TMessage>
        : IMessengerWriter<TMessage>
        where TMessage : class
    {
        Task Completion { get; }
        void Complete();
        ValueTask<bool> WaitToReadAsync(CancellationToken token);
        IAsyncEnumerable<TMessage> ReadAllAsync(CancellationToken token);
    }
}
