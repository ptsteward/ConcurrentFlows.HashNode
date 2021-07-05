using ConcurrentFlows.ProcessManagement.Infrastructure.Dictionaries;
using ConcurrentFlows.ProcessManagement.Infrastructure.Handlers;
using ConcurrentFlows.ProcessManagement.Infrastructure.Messaging;
using ConcurrentFlows.ProcessManagement.Infrastructure.Records;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ConcurrentFlows.ProcessManagement.Infrastructure
{
public static class RegistrationExtensions
{
    public static IServiceCollection AddMessageSystem<TMessage>(this IServiceCollection services)
    {
        var messageSystem = new MessageSystem<TMessage>();
        services.AddSingleton<IMessageSystemWriter<TMessage>>(messageSystem);
        services.AddSingleton<IMessageSystemReader<TMessage>>(messageSystem);
        return services;
    }

    public static IServiceCollection AddCommandHandler<TActor, TCommand>(this IServiceCollection services)
        where TActor : CommandHandler<TCommand> 
        => services
            .AddHostedService<TActor>()
            .AddMessageSystem<TCommand>();

    public static IServiceCollection AddWriterProvider(this IServiceCollection services)
        => services.AddSingleton<IWriterProvider, WriterProvider>();

    public static IServiceCollection AddProcessStartHandler<THandler, TMessageFactory, TStartMessage, TEndMessage, TInput>(this IServiceCollection services)
        where THandler : ProcessStartHandler<TStartMessage, TInput>
        where TMessageFactory : MessageFactory<TInput>
        where TStartMessage : ProcessStartMessage<TInput>
        where TInput : ProcessInput
        => services
            .AddHostedService<THandler>()
            .AddSingleton<IMessageFactory<TInput>, TMessageFactory>()
            .AddMessageSystem<TStartMessage>()
            .AddMessageSystem<TEndMessage>();

    public static IServiceCollection AddPhaseTransitionHandler<THandler, TPhaseTransitions, TMessage, TInput>(this IServiceCollection services)
        where THandler : PhaseTransitionHandler<TMessage, TInput>
        where TPhaseTransitions : PhaseTransitions<TInput>
        where TMessage : ProcessMessage<TInput>
        where TInput : ProcessInput 
        => services
            .AddHostedService<THandler>()
            .AddSingleton<IPhaseTransitions<TInput>, TPhaseTransitions>()
            .AddMessageSystem<TMessage>();
}
}
