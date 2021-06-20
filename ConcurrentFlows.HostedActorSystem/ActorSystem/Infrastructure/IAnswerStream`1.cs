using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace ConcurrentFlows.HostedActorSystem.ActorSystem.Infrastructure
{
    public interface IAnswerStream<TAnswer> : IHostedService
    {
        ValueTask<TAnswer> SubmitQuery<TQuery, TPayload>(TQuery query)
            where TQuery : ActorQuery<TPayload, TAnswer>;
    }
}
