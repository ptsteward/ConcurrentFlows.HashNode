using ConcurrentFlows.MessagingLibrary.Handlers;
using ConcurrentFlows.MessagingLibrary.HostedServices;
using ConcurrentFlows.MessagingLibrary.Interfaces;
using ConcurrentFlows.MessagingLibrary.Model;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace ConcurrentFlows.MessagingLibrary.RegistrationExtensions
{
    public static class MessageRouterRegistrationExtensions
    {
        public static void AddMessageRouter<TEnum, TPayload, TInternalMessage>(this IServiceCollection services,
            Type messageFactory = null,
            Func<IServiceProvider, IMessageFactory<TEnum, TPayload, TInternalMessage>> factoryFactory = null)
            where TEnum : Enum
            where TPayload : class
            where TInternalMessage : InternalMessage<TEnum, TPayload>
        {
            if (messageFactory is null && factoryFactory is null)
                throw new ArgumentException($"Must provide a {nameof(messageFactory)}.");
            if (messageFactory is not null && factoryFactory is not null)
                throw new ArgumentException($"Must only provide one {nameof(messageFactory)}.");
            if (messageFactory is not null &&
                !messageFactory.GetInterfaces().Contains(typeof(IMessageFactory<TEnum, TPayload, TInternalMessage>)))
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
