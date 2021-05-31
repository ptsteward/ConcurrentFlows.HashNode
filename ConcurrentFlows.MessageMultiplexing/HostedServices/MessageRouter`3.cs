using ConcurrentFlows.MessageMultiplexing.Interfaces;
using ConcurrentFlows.MessageMultiplexing.Messages;
using ConcurrentFlows.MessagingLibrary.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrentFlows.MessageMultiplexing.HostedServices
{
    public class MessageRouter<TEnum, TPayload, TInternalMessage>
        : BackgroundService
        where TEnum : Enum
        where TInternalMessage : InternalMessage<TEnum, TPayload>
    {
        private readonly ILogger<MessageRouter<TEnum, TPayload, TInternalMessage>> logger;
        private readonly IMessengerReader<TInternalMessage> messenger;
        private readonly ImmutableDictionary<TEnum, Func<TInternalMessage, IAsyncEnumerable<object>>> messageFactoryMap;
        private readonly IServiceProvider serviceProvider;

        private ConcurrentDictionary<Type, dynamic> writerCache = new ConcurrentDictionary<Type, dynamic>();

        public MessageRouter(
            ILogger<MessageRouter<TEnum, TPayload, TInternalMessage>> logger,
            IMessengerReader<TInternalMessage> messenger,
            IMessageFactory<TEnum, TPayload, TInternalMessage> messageFactory,
            IServiceProvider serviceProvider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            messageFactoryMap = messageFactory?.MessageFactoryMap ?? throw new ArgumentNullException(nameof(messageFactoryMap));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await MultiplexInternalMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await messenger.Shutdown();
            }
        }

        private async ValueTask MultiplexInternalMessagesAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                stoppingToken.ThrowIfCancellationRequested();
                if (await messenger.WaitToReadAsync(stoppingToken))
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    await foreach (var internalMessage in messenger.ReadAllAsync(stoppingToken))
                    {
                        stoppingToken.ThrowIfCancellationRequested();
                        await GenerateMessagesAndWriteAsync(internalMessage);
                    }
                }
            }
        }

        private async ValueTask GenerateMessagesAndWriteAsync(TInternalMessage internalMessage)
        {
            if (messageFactoryMap.ContainsKey(internalMessage.Type))
            {
                var externalMessages = messageFactoryMap[internalMessage.Type](internalMessage);
                await foreach (var externalMessage in externalMessages)
                {
                    var writerType = typeof(IMessengerWriter<>).MakeGenericType(externalMessage.GetType());
                    if (!writerCache.TryGetValue(writerType, out dynamic writer))
                    {
                        writer = serviceProvider.GetRequiredService(writerType);
                        writerCache.TryAdd(writerType, writer);
                    }
                    writer.WriteAsync((dynamic)externalMessage);
                    logger.LogInformation($"Sent {JsonSerializer.Serialize(externalMessage)} to {typeof(IMessengerWriter<>).Name}<{writerType.GenericTypeArguments[0].Name}>");
                }
            }
        }
    }
}
