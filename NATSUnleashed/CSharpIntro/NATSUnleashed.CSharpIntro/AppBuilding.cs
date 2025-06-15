﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.Serializers.Json;

namespace NATSUnleashed.CSharpIntro;

public static class AppBuilding
{
    public const string Publisher = "publisher";
    public const string Subscriber = "subscriber";
    public const string Requestor = "requestor";
    public const string Responder = "responder";

    public static IServiceCollection AddNatsServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.Configure<NatsConfig>(opts =>
        {
            opts.AppType = config["app-type"] ?? "publisher";
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
        return services.AddHostedService<NatsService>();
    }
}
