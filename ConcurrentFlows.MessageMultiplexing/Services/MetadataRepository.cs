using ConcurrentFlows.MessageMultiplexing.Interfaces;
using System.Threading.Tasks;

namespace ConcurrentFlows.MessageMultiplexing.Services
{
    public class MetadataRepository : IMetadataRepository
    {
        public Task<string> GetMetadadataAsync(int id)
            => Task.FromResult("Some metadata");
    }
}
