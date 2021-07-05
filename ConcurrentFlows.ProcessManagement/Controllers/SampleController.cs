using ConcurrentFlows.ProcessManagement.Infrastructure.Messaging;
using ConcurrentFlows.ProcessManagement.SayHello;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrentFlows.ProcessManagement.Controllers
{
[ApiController]
[Route("[controller]")]
public class SampleController : ControllerBase
{
    private readonly IMessageSystemWriter<SayHelloProcessStartMessage> startWriter;
    private readonly IMessageSystemReader<SayHelloResponseMessage> responseReader;

    public SampleController(
        IMessageSystemWriter<SayHelloProcessStartMessage> startWriter,
        IMessageSystemReader<SayHelloResponseMessage> responseReader)
    {
        this.startWriter = startWriter ?? throw new ArgumentNullException(nameof(startWriter));
        this.responseReader = responseReader ?? throw new ArgumentNullException(nameof(responseReader));
    }

    [HttpGet]
    public async ValueTask<SayHelloResponseMessage> Get(string name)
    {
        var startMessage = new SayHelloProcessStartMessage(new SayHelloInput(name));
        await startWriter.WriteAsync(startMessage);
        using var readTokenSource = new CancellationTokenSource();
        readTokenSource.CancelAfter(TimeSpan.FromSeconds(1));
        return await responseReader.ContinuousWaitAndReadAllAsync(readTokenSource.Token).FirstAsync();
    }
}
}
