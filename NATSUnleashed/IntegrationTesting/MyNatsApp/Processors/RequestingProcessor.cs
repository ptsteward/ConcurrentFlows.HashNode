using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace NATSUnleashed.MyNatsApp.Processors;

internal sealed class RequestingProcessor(
    ILogger<RequestingProcessor> logger)
    : IMessageProcessor
{
    private readonly ILogger<RequestingProcessor> _logger = logger;

    public Task ProcessMessageAsync(NatsMsg<ExampleMessage> msg, CancellationToken cancelToken)
    {
        _logger.LogInformation("Req/Rep - Reply [{Reply}]", msg.Data);
        return Task.CompletedTask;
    }
}
