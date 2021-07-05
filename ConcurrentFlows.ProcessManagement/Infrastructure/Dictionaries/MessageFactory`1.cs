using ConcurrentFlows.ProcessManagement.Infrastructure.Records;
using System;
using System.Collections.Generic;

namespace ConcurrentFlows.ProcessManagement.Infrastructure.Dictionaries
{
    public abstract class MessageFactory<TInput>
        : Dictionary<ProcessPhase, Func<Guid, ProcessPhase, TInput, IEnumerable<object>, IAsyncEnumerable<object>>>,
        IMessageFactory<TInput>
        where TInput : ProcessInput
    {

    }
}
