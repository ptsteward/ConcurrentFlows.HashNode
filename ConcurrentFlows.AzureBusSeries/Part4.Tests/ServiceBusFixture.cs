using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Configuration;

namespace ConcurrentFlows.AzureBusSeries.Part4.Tests;

public sealed class ServiceBusFixture
    : IAsyncLifetime
{
    private readonly ServiceBusClient client;

    public IConfiguration Config => BuildConfig();
    public ServiceBusAdministrationClient AdminClient { get; }

    public ServiceBusFixture()
    {
        var credentials = Config.CreateDefaultCredential();
        var hostName = Config["ServiceBusHost"];

        client = new ServiceBusClient(hostName, credentials, new()
        {
            TransportType = ServiceBusTransportType.AmqpWebSockets,
            Identifier = $"Test-Client"
        });

        AdminClient = new ServiceBusAdministrationClient(hostName, credentials, new());
    }

    public ServiceBusReceiver GetReceiver(string queue)
        => client.CreateReceiver(queue, options: new()
        {
            ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete,
            Identifier = $"Test-Receiver"
        });

    private static IConfiguration BuildConfig()
        => new ConfigurationBuilder()
        .AddUserSecrets<ServiceBusFixture>()
        .Build();

    public Task InitializeAsync()
        => Task.CompletedTask;

    public async Task DisposeAsync()
        => await (client?.DisposeAsync() ?? ValueTask.CompletedTask);
}
