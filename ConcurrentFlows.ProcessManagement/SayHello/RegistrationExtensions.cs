using ConcurrentFlows.ProcessManagement.Infrastructure;
using ConcurrentFlows.ProcessManagement.Infrastructure.Records;
using Microsoft.Extensions.DependencyInjection;

namespace ConcurrentFlows.ProcessManagement.SayHello
{
    public static class RegistrationExtensions
    {
        public static IServiceCollection AddSayHelloProcess(this IServiceCollection services)
            => services
            .AddWriterProvider()
            .AddProcessStartHandler<SayHelloStartHandler, SayHelloMessageFactory, SayHelloProcessStartMessage, SayHelloEndedMessage, SayHelloInput>()
            .AddPhaseTransitionHandler<SayHelloPhaseTransistionsHandler, SayHelloPhaseTransitions, SayHelloProcessMessage<ProcessActivity>, SayHelloInput>()
            .AddCommandHandler<SayHelloValidationHandler, SayHelloProcessMessage<SayHelloValidation>>()
            .AddCommandHandler<SayHelloResponseHandler, SayHelloProcessMessage<SayHelloResponseActivity>>()
            .AddMessageSystem<SayHelloResponseMessage>()
            .AddMessageSystem<SayHelloProcessStartedMessage>();
    }
}
