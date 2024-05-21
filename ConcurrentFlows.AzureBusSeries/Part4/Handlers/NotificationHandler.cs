using ConcurrentFlows.AzureBusSeries.Part4.AppModel;
using Microsoft.Extensions.Logging;
using static System.Environment;

namespace ConcurrentFlows.AzureBusSeries.Part4.Handlers;

public sealed class NotificationHandler
    : IMessageHandler<Notification>
{
    private readonly ILogger<NotificationHandler> logger;

    public NotificationHandler(
        ILogger<NotificationHandler> logger)
        => this.logger = logger.ThrowIfNull();

    public Task HandleAsync(Notification message, CancellationToken cancelToken)
    {
        logger.LogInformation("Received Message:{NewLine}{Message}", 
            NewLine, message);
        return Task.CompletedTask;
    }
}
