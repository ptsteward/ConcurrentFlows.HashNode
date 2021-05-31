using System.Threading.Tasks;

namespace ConcurrentFlows.MessageMultiplexing.Interfaces
{
    public interface IMetadataRepository
    {
        Task<string> GetMetadadataAsync(int id);
    }
}
