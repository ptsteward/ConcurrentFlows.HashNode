using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using WebApplication1.Model;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SampleController : ControllerBase
    {
        private readonly IStreamingTransformer<DataInput, DataOutput> transformer;

        public SampleController(IStreamingTransformer<DataInput, DataOutput> transformer)
        {
            this.transformer = transformer ?? throw new ArgumentNullException(nameof(transformer));
        }

        [HttpPost]
        public IAsyncEnumerable<DataOutput> ProcessInput(DataInput input)
            => transformer.SubmitAndReturnResults(input);

        [HttpPost]
        [Route("naive")]
        public async IAsyncEnumerable<DataOutput> ProcessInputNaive(DataInput input)
        {
            foreach (var c in input.Payload)
            {
                yield return new DataOutput() { Result = c };
            }
        }

        [HttpPost]
        [Route("toomuchresponsibility")]
        public async IAsyncEnumerable<DataOutput> TooMuchResponsibility(DataInput input)
        {
            await transformer.Source.WriteAsync(input);
            transformer.Source.Complete();
            while (await transformer.Results.WaitToReadAsync())
            {
                yield return await transformer.Results.ReadAsync();
            }
            await transformer.ExecuteComplete;
        }
    }
}
