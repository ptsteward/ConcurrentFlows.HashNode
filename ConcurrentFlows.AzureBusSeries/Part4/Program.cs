using ConcurrentFlows.AzureBusSeries.Part4;
using ConcurrentFlows.AzureBusSeries.Part4.AppModel;
using ConcurrentFlows.AzureBusSeries.Part4.Handlers;
using ConcurrentFlows.AzureBusSeries.Part4.Reader;
using ConcurrentFlows.AzureBusSeries.Part4.Writer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder();

builder.Configuration.AddUserSecrets<Program>();

builder.Logging.AddConsole();

builder.Services
    .AddHostedService<QueueWriter>()
    .AddHostedService<QueueReader<Notification>>()
    .AddSingleton<IMessageHandler<Notification>, NotificationHandler>()
    .AddAzureBusComponents();

var app = builder.Build();

await app.RunAsync();
