using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConcurrentFlows.AzureBusSeries.Part2;

public class QueueSender
    : BackgroundService
{
    private readonly ILogger<QueueSender> logger;
    private readonly ServiceBusSender sender;

    public QueueSender(
        ILogger<QueueSender> logger,
        ServiceBusSender sender)

    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeout.Token);
        try
        {
            await sender.SendMessageAsync(new("Hello World"), cts.Token);

            logger.LogInformation("Sent Message!!!");
        }
        catch (OperationCanceledException ex) 
            when (timeout.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Operation timed out");
            throw;
        }
        catch (OperationCanceledException ex) 
            when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation(ex, "Shutdown early");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            throw;
        }
    }
}
