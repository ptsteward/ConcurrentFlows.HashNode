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
        private readonly Transformer<DataInput, DataOutput> transformer;

        public SampleController(Transformer<DataInput, DataOutput> transformer)
        {
            this.transformer = transformer ?? throw new ArgumentNullException(nameof(transformer));
        }

        [HttpPost]
        public IAsyncEnumerable<DataOutput> ProcessInput(DataInput input) 
            => transformer.SubmitAndReturnResults(input);
    }
}
