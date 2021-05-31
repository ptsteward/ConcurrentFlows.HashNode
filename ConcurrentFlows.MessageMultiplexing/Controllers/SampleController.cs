using ConcurrentFlows.MessageMultiplexing.Model;
using ConcurrentFlows.MessageMultiplexing.Model.Messages.Internal;
using ConcurrentFlows.MessagingLibrary.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ConcurrentFlows.MessageMultiplexing.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SampleController : ControllerBase
    {
        public readonly IMessengerWriter<SampleHubInternalMessage> writer;

        public SampleController(IMessengerWriter<SampleHubInternalMessage> writer)
        {
            this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        [HttpGet]
        public async Task<IActionResult> Get(string input)
        {
            var internalMessage = new SampleHubInternalMessage(SampleHubMessageType.Created, new SampleEntity(1, input));
            await writer.WriteAsync(internalMessage);
            return Ok();
        }
    }
}
