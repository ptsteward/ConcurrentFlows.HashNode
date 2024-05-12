using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConcurrentFlows.AzureBusSeries.Part4.Tests;

public sealed class ServiceBusSenderTests
    : IClassFixture<ServiceBusFixture>,
    IAsyncLifetime
{
    private readonly ServiceBusAdministrationClient adminClient;
    private readonly IConfiguration config;
    private readonly ServiceBusReceiver receiver;
    private readonly string testQueue;
    private QueueProperties queueProps = default!;

    public ServiceBusSenderTests(ServiceBusFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        adminClient = fixture.AdminClient
            ?? throw new ArgumentNullException(nameof(fixture.AdminClient));
        config = fixture.Config
            ?? throw new ArgumentNullException(nameof(fixture.Config));

        testQueue = $"Test-{Guid.NewGuid()}";
        receiver = fixture.GetReceiver(testQueue);
    }

    [Fact]
    public async Task Can_Send_And_Receive_Message()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        config["Queue"] = testQueue;
        await adminClient.CreateQueueAsync(new CreateQueueOptions(queueProps)
        {
            Name = testQueue
        }, cts.Token);

        var sender = config.BuildQueueSender();
        await sender.StartAsync(cts.Token);

        var scheduledMsgs = await adminClient.LookForScheduledMessagesAsync(testQueue, cts.Token);

        Assert.Equal(1, scheduledMsgs);

        await sender.StopAsync(cts.Token);
        await sender.ExecuteTask!;
    }

    

    public async Task InitializeAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var queueName = config["Queue"];
        var propsResponse = await adminClient.GetQueueAsync(queueName, cts.Token);
        queueProps = propsResponse.Value;
    }

    public async Task DisposeAsync()
    {
        if (adminClient is null)
            return;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var queueExists = await adminClient.QueueExistsAsync(testQueue, cts.Token);
        if (queueExists)
            await adminClient.DeleteQueueAsync(testQueue, cts.Token);
    }
}