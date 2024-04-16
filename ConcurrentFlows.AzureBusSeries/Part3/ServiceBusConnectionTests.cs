using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Configuration;

namespace ConcurrentFlows.AzureBusSeries.Part3;

public sealed class ServiceBusConnectionTests
    : IClassFixture<ServiceBusFixture>
{
    private readonly IConfiguration config;
    private readonly ServiceBusAdministrationClient adminClient;

    public ServiceBusConnectionTests(ServiceBusFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        config = fixture.Config
            ?? throw new ArgumentNullException(nameof(fixture.Config));
        adminClient = fixture.AdminClient
            ?? throw new ArgumentNullException(nameof(fixture.AdminClient));
    }

    [Fact]
    public async Task Can_Connect_And_Queue_Exists()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));        
        var queueName = config["Queue"];

        var result = await adminClient.QueueExistsAsync(queueName, cts.Token);

        Assert.True(result);
    }
}
