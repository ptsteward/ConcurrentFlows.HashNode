using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ConcurrentFlows.AzureBusSeries.Part4;

public static class Extensions
{
    private const string Identifier = "AzureBusSeries";

    public static IServiceCollection AddServiceBusForQueueSender(
        this IServiceCollection services)
        => services
        .AddSingleton(sp =>
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
        })
        .AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var client = sp.GetRequiredService<ServiceBusClient>();
            var queue = config["Queue"];

            var options = new ServiceBusSenderOptions()
            {
                Identifier = $"{Identifier}-Sender"
            };

            return client.CreateSender(queue, options);
        })
        .AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var client = sp.GetRequiredService<ServiceBusClient>();
            var queue = config["Queue"];

            var options = new ServiceBusReceiverOptions()
            {
                Identifier = $"{Identifier}-Reader"
            };

            return client.CreateReceiver(queue, options);
        });

    public static DefaultAzureCredential CreateDefaultCredential(this IConfiguration config)
    {
        var tenantId = config["TenantId"];
        return new(new DefaultAzureCredentialOptions()
        {
            TenantId = tenantId
        }.SetVisualStudioCredentialingOnly());
    }

    public static DefaultAzureCredentialOptions SetVisualStudioCredentialingOnly(this DefaultAzureCredentialOptions options)
    {
        options.ExcludeAzureCliCredential = true;
        options.ExcludeAzureDeveloperCliCredential = true;
        options.ExcludeAzurePowerShellCredential = true;
        options.ExcludeEnvironmentCredential = true;
        options.ExcludeInteractiveBrowserCredential = true;
        options.ExcludeVisualStudioCodeCredential = true;
        options.ExcludeWorkloadIdentityCredential = true;
        options.ExcludeManagedIdentityCredential = true;
        options.ExcludeSharedTokenCacheCredential = true;
        return options;
    }

    [return: NotNullIfNotNull(nameof(item))]
    public static T ThrowIfNull<T>(
        [NotNull] this T? item, 
        [CallerArgumentExpression(nameof(item))] string? paramName = null)
    {
        if (item is null)
            throw new ArgumentNullException(paramName);
        return item; 
    }
}
