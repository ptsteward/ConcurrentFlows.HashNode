using ConcurrentFlows.ProcessManagement.Infrastructure.Dictionaries;
using ConcurrentFlows.ProcessManagement.Infrastructure.Messaging;
using ConcurrentFlows.ProcessManagement.Infrastructure.Records;
using System.Collections.Generic;

namespace ConcurrentFlows.ProcessManagement.SayHello
{
    public class SayHelloPhaseTransitions : PhaseTransitions<SayHelloInput>
    {
        public SayHelloPhaseTransitions()
        {
            this[SayHelloPhases.Validation] = (phase, input, messages) => ValidationPhaseTransition(phase, input, messages);
            this[SayHelloPhases.SayHello] = (phase, input, messages) => SayHelloPhaseTransition(phase, input, messages);
        }

        public ProcessPhase ValidationPhaseTransition(ProcessPhase phase, SayHelloInput input, IEnumerable<dynamic> currentMessages)
        {
            if (currentMessages.ContainsActivity<SayHelloValidationSuccess>())
                return SayHelloPhases.SayHello;
            else if (currentMessages.ContainsActivity<SayHelloValidationFailure>())
                return SayHelloPhases.Failed;
            return phase;
        }

        public ProcessPhase SayHelloPhaseTransition(ProcessPhase phase, SayHelloInput input, IEnumerable<dynamic> currentMessages)
        {
            if (currentMessages.ContainsActivity<SayHelloCompletedActivity>())
                return SayHelloPhases.Completed;
            return phase;
        }
    }
}
