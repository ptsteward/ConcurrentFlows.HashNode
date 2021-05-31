using ConcurrentFlows.MessageHandling.Interfaces;
using ConcurrentFlows.MessageHandling.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrentFlows.MessageHandling.HostedServices
{
    public class BackgroundMessenger<TMessage>
        : BackgroundService
        where TMessage : class
    {
        private readonly ILogger<BackgroundMessenger<TMessage>> logger;
        private readonly IMessenger<TMessage> messenger;
        private readonly IEnumerable<IPublisher<TMessage>> publishers;

        public BackgroundMessenger(
            ILogger<BackgroundMessenger<TMessage>> logger,
            IMessenger<TMessage> messenger,
            IEnumerable<IPublisher<TMessage>> publishers)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            this.publishers = publishers ?? throw new ArgumentNullException(nameof(publishers));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await ReadAndPublish(stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await messenger.Shutdown();
            }
        }

        private async Task ReadAndPublish(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                stoppingToken.ThrowIfCancellationRequested();
                if (await messenger.WaitToReadAsync(stoppingToken))
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    await foreach (var message in messenger.ReadAllAsync(stoppingToken))
                    {
                        stoppingToken.ThrowIfCancellationRequested();
                        await Task.WhenAll(publishers.Select(publisher => TryPublish(publisher, message)));
                    }
                }
            }
        }

        private async Task TryPublish(IPublisher<TMessage> publisher, TMessage message)
        {
            try
            {
                await publisher.PublishAsync(message);
                logger.LogInformation($"Published to {publisher.GetType().Name} with message {JsonSerializer.Serialize(message)}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error occurred while publishing to {publisher.GetType().Name} with message {JsonSerializer.Serialize(message)}");
            }
        }
    }
}
