using System.Threading;
using System.Threading.Tasks;

namespace ConcurrentFlows.ProcessManagement.Infrastructure.Messaging
{
    public interface IMessageSystemWriter<in TMessage>
    {
        public ValueTask WriteAsync(TMessage message, CancellationToken token = default);
        public Task Shutdown();
    }
}
