using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NATSUnleashed.MyNatsApp.Services;

public sealed class NatsService(
    ILogger<NatsService> logger,
    IOptions<NatsConfig> options,
    INatsServiceBehavior behavior)
    : BackgroundService
{
    private readonly IOptions<NatsConfig> _options = options;
    private readonly ILogger<NatsService> _logger = logger;
    private readonly INatsServiceBehavior _behavior = behavior;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Service} Configured As: {AppType} - With Behavior {Behavior}",
            GetType().Name,
            _options.Value.AppType,
            _behavior.GetType().Name);
        await _behavior.ExecuteAsync(stoppingToken);
    }
}
