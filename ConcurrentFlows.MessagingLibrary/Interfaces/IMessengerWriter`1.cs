using System.Threading.Tasks;

namespace ConcurrentFlows.MessagingLibrary.Interfaces
{
    public interface IMessengerWriter<TMessage> where TMessage : class
    {
        ValueTask WriteAsync(TMessage message);
    }
}
