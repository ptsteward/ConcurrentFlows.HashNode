using Azure.Messaging.ServiceBus;
using Bogus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static System.Threading.CancellationTokenSource;

namespace ConcurrentFlows.AzureBusSeries.Part4.Writer;

public sealed class QueueWriter
    : BackgroundService,
    IAsyncDisposable
{
    private readonly ILogger<QueueWriter> logger;
    private readonly ServiceBusSender sender;
    private readonly int count;
    private readonly Faker faker = new();

    public QueueWriter(
        ILogger<QueueWriter> logger,
        ServiceBusSender sender,
        int count = 5)
    {
        this.logger = logger.ThrowIfNull();
        this.sender = sender.ThrowIfNull();
        this.count = count;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var cts = CreateLinkedTokenSource(stoppingToken);

        var messages = Enumerable.Range(1, count)
            .Select(id =>
            {
                var name = faker.Name.FirstName();
                var notification = new Notification(id, $"Hello from {name}");
                var body = JsonSerializer.Serialize(notification);
                var message = new ServiceBusMessage(body);
                return message;
            });

        await sender.SendMessagesAsync(messages, stoppingToken);
        logger.LogInformation("Finished");
    }

    public async ValueTask DisposeAsync()
    {
        await sender.DisposeAsync();
        base.Dispose();        
    }
}
