using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;

namespace NATSUnleashed.MyNatsApp.Behaviors;

public sealed class SubscribingBehavior(
    ILogger<NatsService> logger,
    INatsConnection connection,
    IOptions<NatsConfig> options,
    IMessageProcessor processor)
    : INatsServiceBehavior
{
    private readonly ILogger<NatsService> _logger = logger;
    private readonly INatsConnection _connection = connection;
    private readonly IOptions<NatsConfig> _options = options;
    private readonly IMessageProcessor _processor = processor;

    public async Task ExecuteAsync(CancellationToken cancelToken)
    {
        var (_, _, _, subject, group) = _options.Value;
        _logger.LogInformation("Subscribing to {Subject}", subject);
        await foreach (var msg in _connection.SubscribeAsync<ExampleMessage>(
            subject,
            queueGroup: group,
            cancellationToken: cancelToken))
            await _processor.ProcessMessageAsync(msg, cancelToken);
    }
}
