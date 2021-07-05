using ConcurrentFlows.ProcessManagement.Infrastructure.Records;
using System;
using System.Collections.Generic;

namespace ConcurrentFlows.ProcessManagement.Infrastructure.Dictionaries
{
    public abstract class PhaseTransitions<TInput>
        : Dictionary<ProcessPhase, Func<ProcessPhase, TInput, IEnumerable<object>, ProcessPhase>>,
        IPhaseTransitions<TInput>
        where TInput : ProcessInput
    {

    }
}
