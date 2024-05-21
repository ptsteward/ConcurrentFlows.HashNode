using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConcurrentFlows.AzureBusSeries.Part4;

public static class Registrations
{
    private const string Identifier = "AzureBusSeries";

    public static IServiceCollection AddAzureBusComponents(
        this IServiceCollection services)
        => services
        .AddAzureBusClient()
        .AddAzureBusSender()
        .AddAzureBusProcessor();

    private static IServiceCollection AddAzureBusClient(
        this IServiceCollection services)
        => services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            var credential = config.CreateDefaultCredential();
            var hostName = config["ServiceBusHost"];

            var options = new ServiceBusClientOptions()
            {
                TransportType = ServiceBusTransportType.AmqpWebSockets,
                Identifier = $"{Identifier}-Client"
            };

            return new ServiceBusClient(hostName, credential, options);
        });

    private static IServiceCollection AddAzureBusSender(
        this IServiceCollection services)
        => services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var client = sp.GetRequiredService<ServiceBusClient>();
            var queue = config["Queue"];

            var options = new ServiceBusSenderOptions()
            {
                Identifier = $"{Identifier}-Writer"
            };

            return client.CreateSender(queue, options);
        });

    private static IServiceCollection AddAzureBusProcessor(
        this IServiceCollection services)
        => services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var client = sp.GetRequiredService<ServiceBusClient>();
            var queue = config["Queue"];

            var options = new ServiceBusProcessorOptions()
            {
                Identifier = $"{Identifier}-Reader"
            };

            return client.CreateProcessor(queue, options);
        });
}
