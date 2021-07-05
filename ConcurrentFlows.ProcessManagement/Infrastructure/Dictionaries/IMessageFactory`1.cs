using ConcurrentFlows.ProcessManagement.Infrastructure.Records;
using System;
using System.Collections.Generic;

namespace ConcurrentFlows.ProcessManagement.Infrastructure.Dictionaries
{
    public interface IMessageFactory<TInput>
        : IReadOnlyDictionary<ProcessPhase, Func<Guid, ProcessPhase, TInput, IEnumerable<object>, IAsyncEnumerable<object>>>
        where TInput : ProcessInput
    {

    }
}
