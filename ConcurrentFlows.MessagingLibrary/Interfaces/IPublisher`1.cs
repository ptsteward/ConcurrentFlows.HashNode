using System.Threading.Tasks;

namespace ConcurrentFlows.MessagingLibrary.Interfaces
{
    public interface IPublisher<TMessage> where TMessage : class
    {
        Task PublishAsync(TMessage message);
    }
}
