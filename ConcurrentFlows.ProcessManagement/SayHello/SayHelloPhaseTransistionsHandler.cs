using ConcurrentFlows.ProcessManagement.Infrastructure.Dictionaries;
using ConcurrentFlows.ProcessManagement.Infrastructure.Handlers;
using ConcurrentFlows.ProcessManagement.Infrastructure.Messaging;
using ConcurrentFlows.ProcessManagement.Infrastructure.Records;
using System;

namespace ConcurrentFlows.ProcessManagement.SayHello
{
    public class SayHelloPhaseTransistionsHandler : PhaseTransitionHandler<SayHelloProcessMessage<ProcessActivity>, SayHelloInput>
    {
        public SayHelloPhaseTransistionsHandler(
            IWriterProvider writerProvider,
            IPhaseTransitions<SayHelloInput> phaseTransitions,
            IMessageFactory<SayHelloInput> messageFactory,
            IMessageSystemReader<SayHelloProcessMessage<ProcessActivity>> reader)
            : base(writerProvider, phaseTransitions, messageFactory, reader)
        {
        }

        protected override ProcessEndedMessage EndedMessageFactory(Guid processId, ProcessPhase phase)
            => new SayHelloEndedMessage(processId, phase);
    }
}
