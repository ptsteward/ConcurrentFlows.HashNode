using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConcurrentFlows.MessageHandling.Services
{
    public interface IPublisher<TMessage> where TMessage : class
    {
        Task PublishAsync(TMessage message);
    }
}
