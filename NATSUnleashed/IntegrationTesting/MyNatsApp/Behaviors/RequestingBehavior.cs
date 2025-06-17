using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;

namespace NATSUnleashed.MyNatsApp.Behaviors;

public sealed class RequestingBehavior(
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
        var (_, _, maxMessages, subject, _) = _options.Value;
        _logger.LogInformation("Requesting to {Subject}", subject);
        var msgCount = 0;
        while (!cancelToken.IsCancellationRequested && ShouldContinue(maxMessages, msgCount))
        {
            var message = Random.Shared.NextRequestMessage();
            var reply = await _connection.RequestAsync<ExampleMessage, ExampleMessage>(
                subject,
                message,
                cancellationToken: cancelToken);
            await _processor.ProcessMessageAsync(reply, cancelToken);
            msgCount++;
        }
    }

    private static bool ShouldContinue(int? max, int count)
        => max switch
        {
            null => true,
            _ => count < max
        };
}
