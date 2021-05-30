using System.Threading.Tasks;

namespace ConcurrentFlows.MessageHandling.Interfaces
{
    public interface IMessengerWriter<TMessage> where TMessage : class
    {
        ValueTask WriteAsync(TMessage message);
    }
}
