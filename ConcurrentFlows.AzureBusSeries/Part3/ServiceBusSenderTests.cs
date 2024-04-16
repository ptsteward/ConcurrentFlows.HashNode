using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ConcurrentFlows.AzureBusSeries.Part2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConcurrentFlows.AzureBusSeries.Part3;

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

        var sender = BuildQueueSender(config);
        await sender.StartAsync(cts.Token);

        var msg = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2), cts.Token);

        Assert.NotNull(msg);
        var expecetd = "Hello World";
        Assert.Equal(expecetd, $"{msg.Body}");

        await sender.StopAsync(cts.Token);
    }

    private QueueSender BuildQueueSender(IConfiguration config)
        => new ServiceCollection()
        .AddSingleton(config)
        .AddSingleton<QueueSender>()
        .AddServiceBusForQueueSender()
        .AddLogging()
        .BuildServiceProvider()
        .GetRequiredService<QueueSender>();

    public async Task InitializeAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var queueName = config["Queue"];
        var propsResponse = await adminClient.GetQueueAsync(queueName, cts.Token);
        queueProps = propsResponse.Value;
    }

    public async Task DisposeAsync()
    {
        if (adminClient is null)
            return;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var queueExists = await adminClient.QueueExistsAsync(testQueue, cts.Token);
        if (queueExists)
            await adminClient.DeleteQueueAsync(testQueue, cts.Token);
    }
}