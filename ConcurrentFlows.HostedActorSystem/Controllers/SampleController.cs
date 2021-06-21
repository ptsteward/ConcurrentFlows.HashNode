using ConcurrentFlows.HostedActorSystem.ActorSystem.Infrastructure;
using ConcurrentFlows.HostedActorSystem.Queries.Actor;
using ConcurrentFlows.HostedActorSystem.Queries.Api;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ConcurrentFlows.HostedActorSystem.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SampleController : ControllerBase
    {
        private readonly IAnswerStream answerStream;

        public SampleController(IAnswerStream answerStream)
            => this.answerStream = answerStream ?? throw new ArgumentNullException(nameof(answerStream));

        [HttpGet]
        public async Task<IActionResult> GetFactorial([FromQuery] GetFactorialApiQuery query)
        {
            var actorQuery = new GetFactorialActorQuery(query.Message);
            var result = await answerStream.SubmitQuery(actorQuery);
            return Ok(result);
        }

        [HttpGet("message")]
        public async Task<IActionResult> ReturnMessage([FromQuery] GetMessageApiQuery query)
        {
            var actorQuery = new GetMessageActorQuery(query.Message);
            var result = await answerStream.SubmitQuery(actorQuery);
            return Ok(result);
        }
    }
}
