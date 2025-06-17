using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.Serializers.Json;

namespace NATSUnleashed.MyNatsApp;

public static class AppBuilding
{
    public const string Publisher = "publisher";
    public const string Subscriber = "subscriber";
    public const string Requestor = "requestor";
    public const string Responder = "responder";

    public static IServiceCollection AddAppServices(
        this IServiceCollection services,
        IConfiguration config)
        => services
        .AddNatsInfrastructure(config)
        .AddBehavior(config)
        .AddProcessor(config)
        .AddHostedService<NatsService>();

    private static IServiceCollection AddNatsInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.Configure<NatsConfig>(opts =>
        {
            opts.AppType = config["app:type"] ?? "publisher";
            opts.MaxMessages = config.GetValue<int?>("app:maxMessages");
            opts.NatsUrl = config["nats:url"] ?? "nats://localhost:4222";
            opts.Subject = config["nats:subject"] ?? "some.subject";
            opts.QueueGroup = config["nats:group"];
        });
        services.TryAddSingleton<INatsConnection>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<NatsConfig>>().Value;
            return new NatsConnection(new()
            {
                Url = config.NatsUrl,
                SerializerRegistry = NatsJsonSerializerRegistry.Default
            });
        });
        return services;
    }

    private static IServiceCollection AddBehavior(
        this IServiceCollection services,
        IConfiguration config)
        => config["app:type"] switch
        {
            Publisher => services.AddSingleton<INatsServiceBehavior, PublishingBehavior>(),
            Subscriber => services.AddSingleton<INatsServiceBehavior, SubscribingBehavior>(),
            Requestor => services.AddSingleton<INatsServiceBehavior, RequestingBehavior>(),
            Responder => services.AddSingleton<INatsServiceBehavior, RespondingBehavior>(),
            _ => throw new NotSupportedException($"App type {config["app:type"]} is not supported.")
        };

    private static IServiceCollection AddProcessor(
        this IServiceCollection services,
        IConfiguration config)
        => config["app:type"] switch
        {
            Publisher => services,
            Subscriber => services.AddSingleton<IMessageProcessor, SubscribingProcessor>(),
            Requestor => services.AddSingleton<IMessageProcessor, RequestingProcessor>(),
            Responder => services.AddSingleton<IMessageProcessor, RespondingProcessor>(),
            _ => throw new NotSupportedException($"App type {config["app:type"]} is not supported.")
        };
}
