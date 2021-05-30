using ConcurrentFlows.MessageHandling.Messages;
using System.Threading.Tasks;

namespace ConcurrentFlows.MessageHandling.Interfaces
{
    public interface ISampleHubClient
    {
        Task ClientEvent(EventMessage message);
    }
}
