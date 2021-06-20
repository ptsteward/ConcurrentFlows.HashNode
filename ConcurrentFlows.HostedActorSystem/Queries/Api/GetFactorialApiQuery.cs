using Microsoft.AspNetCore.Mvc;

namespace ConcurrentFlows.HostedActorSystem.Queries.Api
{
    public record GetFactorialApiQuery([FromQuery] int Message);
}
