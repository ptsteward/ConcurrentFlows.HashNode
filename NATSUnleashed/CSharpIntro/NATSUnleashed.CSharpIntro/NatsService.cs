using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;

namespace NATSUnleashed.CSharpIntro;

public sealed class NatsService(
    ILogger<NatsService> logger,
    INatsConnection connection,
    IOptions<NatsConfig> options)
    : BackgroundService
{
    private readonly ILogger<NatsService> _logger = logger;
    private readonly INatsConnection _connection = connection;
    private readonly IOptions<NatsConfig> _options = options;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => _options.Value.AppType switch
        {
            AppBuilding.Publisher => PublisherAsync(stoppingToken),
            AppBuilding.Subscriber => SubscriberAsync(stoppingToken),
            AppBuilding.Requestor => RequestorAsync(stoppingToken),
            AppBuilding.Responder => ResponderAsync(stoppingToken),
            _ => throw new NotSupportedException($"App type {_options.Value.AppType} is not supported.")
        };

    private async Task PublisherAsync(CancellationToken cancelToken)
    {
        var (_, _, subject, _) = _options.Value;
        _logger.LogInformation("Publishing to {Subject}", subject);
        while (!cancelToken.IsCancellationRequested)
        {
            var message = Random.Shared.NextPubSubMessage();
            await _connection.PublishAsync(
                subject,
                message,
                cancellationToken: cancelToken);

            await Task.Delay(TimeSpan.FromSeconds(3), cancelToken);
        }
    }

    private async Task SubscriberAsync(CancellationToken cancelToken)
    {
        var (_, _, subject, group) = _options.Value;
        _logger.LogInformation("Subscribing to {Subject}", subject);
        await foreach (var msg in _connection.SubscribeAsync<ExampleMessage>(
            subject,
            queueGroup: group,
            cancellationToken: cancelToken))
            _logger.LogInformation("Pub/Sub - Received [{Message}]", msg.Data);
    }

    private async Task RequestorAsync(CancellationToken cancelToken)
    {
        var (_, _, subject, _) = _options.Value;
        _logger.LogInformation("Requesting to {Subject}", subject);
        while (!cancelToken.IsCancellationRequested)
        {
            var message = Random.Shared.NextRequestMessage();
            var reply = await _connection.RequestAsync<ExampleMessage, ExampleMessage>(
                subject,
                message,
                cancellationToken: cancelToken);
            _logger.LogInformation("Req/Rep - Reply [{Reply}]", reply.Data);

            await Task.Delay(TimeSpan.FromSeconds(3), cancelToken);
        }
    }

    private async Task ResponderAsync(CancellationToken cancelToken)
    {
        var (_, _, subject, _) = _options.Value;
        _logger.LogInformation("Subscribing to {Subject}", subject);
        await foreach (var msg in _connection.SubscribeAsync<ExampleMessage>(
            subject,
            cancellationToken: cancelToken))
        {
            _logger.LogInformation("Req/Rep - Request [{Message}]", msg.Data);
            var message = Random.Shared.NextReplyMessage();
            await msg.ReplyAsync(message, cancellationToken: cancelToken);
        }
    }
}
