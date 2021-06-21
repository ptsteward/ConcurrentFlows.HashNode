using Microsoft.AspNetCore.Mvc;

namespace ConcurrentFlows.HostedActorSystem.Queries.Api
{
    public record GetMessageApiQuery([FromQuery]string Message);
}
