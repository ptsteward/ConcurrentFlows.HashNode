using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;

namespace NATSUnleashed.MyNatsApp.Behaviors;

public sealed class PublishingBehavior(
    ILogger<NatsService> logger,
    INatsConnection connection,
    IOptions<NatsConfig> options)
    : INatsServiceBehavior
{
    private readonly ILogger<NatsService> _logger = logger;
    private readonly INatsConnection _connection = connection;
    private readonly IOptions<NatsConfig> _options = options;

    public async Task ExecuteAsync(CancellationToken cancelToken)
    {
        var (_, _, maxMessages, subject, _) = _options.Value;
        _logger.LogInformation("Publishing to {Subject}", subject);
        var msgCount = 0;
        while (!cancelToken.IsCancellationRequested && ShouldContinue(maxMessages, msgCount))
        {
            var message = Random.Shared.NextPubSubMessage();
            await _connection.PublishAsync(
                subject,
                message,
                cancellationToken: cancelToken);
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
