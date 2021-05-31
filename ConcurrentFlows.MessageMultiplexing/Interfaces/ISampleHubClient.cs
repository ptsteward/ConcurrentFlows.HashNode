using ConcurrentFlows.MessageMultiplexing.Model.Messages.External;
using System.Threading.Tasks;

namespace ConcurrentFlows.MessageMultiplexing.Interfaces
{
    public interface ISampleHubClient
    {
        Task EntityCreated(EntityCreatedMessage message);
        Task EntityUpdated(EntityUpdatedMessage message);
        Task EntityDeleted(EntityDeletedMessage message);
    }
}
