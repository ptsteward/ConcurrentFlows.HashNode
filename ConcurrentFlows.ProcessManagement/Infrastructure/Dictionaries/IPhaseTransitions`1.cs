using ConcurrentFlows.ProcessManagement.Infrastructure.Records;
using System;
using System.Collections.Generic;

namespace ConcurrentFlows.ProcessManagement.Infrastructure.Dictionaries
{
    public interface IPhaseTransitions<TInput>
        : IReadOnlyDictionary<ProcessPhase, Func<ProcessPhase, TInput, IEnumerable<object>, ProcessPhase>>
        where TInput : ProcessInput
    {

    }
}
