using System.Threading.Tasks;
using System.Threading.Channels;

namespace WebApplication1.Services
{
    public interface IStreamingTransformer<TInput, TOutput>
    {
        public Task ExecuteComplete { get; }
        public ChannelReader<TOutput> Results { get; }
        public ChannelWriter<TInput> Source { get; }
    }
}
