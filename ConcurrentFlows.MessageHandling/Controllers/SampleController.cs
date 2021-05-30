﻿using ConcurrentFlows.MessageHandling.Interfaces;
using ConcurrentFlows.MessageHandling.Messages;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ConcurrentFlows.MessageHandling.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SampleController : ControllerBase
    {
        private readonly IMessengerWriter<EventMessage> writer;

        public SampleController(IMessengerWriter<EventMessage> writer)
        {
            this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        [HttpGet]
        public async Task<IActionResult> Get(string input)
        {
            var msg = new EventMessage()
            {
                Payload = input
            };
            await writer.WriteAsync(msg);
            return Ok();
        }
    }
}