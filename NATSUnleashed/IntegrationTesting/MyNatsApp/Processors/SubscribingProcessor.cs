using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace NATSUnleashed.MyNatsApp.Processors;

internal sealed class SubscribingProcessor(
    ILogger<SubscribingProcessor> logger)
    : IMessageProcessor
{
    private readonly ILogger<SubscribingProcessor> _logger = logger;

    public Task ProcessMessageAsync(NatsMsg<ExampleMessage> msg, CancellationToken cancelToken)
    {
        _logger.LogInformation("Pub/Sub - Received [{Message}]", msg.Data);
        return Task.CompletedTask;
    }
}
