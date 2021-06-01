using ConcurrentFlows.MessagingLibrary.Handlers;
using ConcurrentFlows.MessagingLibrary.HostedServices;
using ConcurrentFlows.MessagingLibrary.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConcurrentFlows.MessagingLibrary.RegistrationExtensions
{
    public static class MessengerRegistration
    {
        public static void AddMessenger<TMessage>(
            this IServiceCollection services,
            IEnumerable<Type> publishers = null,
            IEnumerable<IPublisher<TMessage>> instances = null,
            IEnumerable<Func<IServiceProvider, IPublisher<TMessage>>> factories = null)
            where TMessage : class
        {
            if ((publishers is null || !publishers.Any() || !publishers.All(p => p.GetInterfaces().Contains(typeof(IPublisher<TMessage>)))) &&
                (instances is null || !instances.Any()) &&
                (factories is null || !factories.Any()))
                throw new ArgumentException($"Must register at least one publisher for {typeof(TMessage).Name}");

            publishers ??= Enumerable.Empty<Type>();
            instances ??= Enumerable.Empty<IPublisher<TMessage>>();
            factories ??= Enumerable.Empty<Func<IServiceProvider, IPublisher<TMessage>>>();

            foreach (var publisher in publishers)
                services.AddSingleton(typeof(IPublisher<TMessage>), publisher);
            foreach (var publisher in instances)
                services.AddSingleton(publisher);
            foreach (var factory in factories)
                services.AddSingleton(factory);
            services.AddSingleton<Messenger<TMessage>>();
            services.AddSingleton<IMessengerWriter<TMessage>>(sp => sp.GetRequiredService<Messenger<TMessage>>());
            services.AddSingleton<IMessengerReader<TMessage>>(sp => sp.GetRequiredService<Messenger<TMessage>>());
            services.AddHostedService<BackgroundMessenger<TMessage>>();
        }
    }    
}
