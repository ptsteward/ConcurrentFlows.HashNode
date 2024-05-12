using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace ConcurrentFlows.AzureBusSeries.Part4.Tests;

public static class Extensions
{
    public static QueueSender BuildQueueSender(
        this IConfiguration config)
        => new ServiceCollection()
        .AddSingleton(config)
        .AddSingleton<QueueSender>()
        .AddServiceBusForQueueSender()
        .AddLogging()
        .BuildServiceProvider()
        .GetRequiredService<QueueSender>();

    public static async Task<long> LookForScheduledMessagesAsync(
        this ServiceBusAdministrationClient adminClient,
        string queue,
        CancellationToken cancelToken)
    {
        var activeMessages = 0L;
        while (!cancelToken.IsCancellationRequested && activeMessages == 0) 
        {
            activeMessages = await adminClient.GetScheduledMessageCountAsync(queue, cancelToken);
            
            if (activeMessages != 0)
                break;
            
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
        return activeMessages;
    }

    private static async Task<long> GetScheduledMessageCountAsync(
        this ServiceBusAdministrationClient adminClient,
        string queue,
        CancellationToken cancelToken)
    {
        var response = await adminClient.GetQueueRuntimePropertiesAsync(queue, cancelToken);
        var props = response.Value;
        return props.ScheduledMessageCount;
    }
}
