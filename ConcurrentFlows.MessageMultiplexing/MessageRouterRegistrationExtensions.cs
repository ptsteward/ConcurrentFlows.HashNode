using ConcurrentFlows.MessageMultiplexing.HostedServices;
using ConcurrentFlows.MessageMultiplexing.Interfaces;
using ConcurrentFlows.MessageMultiplexing.Messages;
using ConcurrentFlows.MessagingLibrary.Handlers;
using ConcurrentFlows.MessagingLibrary.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace ConcurrentFlows.MessageMultiplexing
{
    public static class MessageRouterRegistrationExtensions
    {
        public static void AddMessageRouter<TEnum, TPayload, TInternalMessage>(this IServiceCollection services,
            Type messageFactory = null,
            Func<IServiceProvider, IMessageFactory<TEnum, TPayload, TInternalMessage>> factoryFactory = null)
            where TEnum : Enum
            where TInternalMessage : InternalMessage<TEnum, TPayload>
        {
            if (messageFactory is null && factoryFactory is null)
                throw new ArgumentException("Must provide a MessageFactory.");
            if (!messageFactory.GetInterfaces().Contains(typeof(IMessageFactory<TEnum, TPayload, TInternalMessage>)))
                throw new ArgumentException($"{nameof(messageFactory)} must of type {typeof(IMessageFactory<,,>).Name}<{typeof(TEnum).Name},{typeof(TPayload).Name},{typeof(TInternalMessage).Name}>");

            if (messageFactory is not null)
                services.AddSingleton(typeof(IMessageFactory<TEnum, TPayload, TInternalMessage>), messageFactory);
            else
                services.AddSingleton(factoryFactory);
            
            services.AddSingleton<Messenger<TInternalMessage>>();
            services.AddSingleton<IMessengerWriter<TInternalMessage>>(sp => sp.GetRequiredService<Messenger<TInternalMessage>>());
            services.AddSingleton<IMessengerReader<TInternalMessage>>(sp => sp.GetRequiredService<Messenger<TInternalMessage>>());
            services.AddHostedService<MessageRouter<TEnum, TPayload, TInternalMessage>>();
        }
    }
}
