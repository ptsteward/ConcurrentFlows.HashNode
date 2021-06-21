using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace ConcurrentFlows.HostedActorSystem.ActorSystem.Infrastructure
{
    public interface IAnswerStream : IHostedService
    {
        ValueTask<dynamic> SubmitQuery<TQuery>(TQuery query);
    }
}
