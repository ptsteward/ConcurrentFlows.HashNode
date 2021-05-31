using ConcurrentFlows.MessageMultiplexing.Messages;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ConcurrentFlows.MessageMultiplexing.Interfaces
{
    public interface IMessageFactory<TEnum, TPayload, TInternalMessage>        
        where TEnum : Enum
        where TPayload : class
        where TInternalMessage : InternalMessage<TEnum, TPayload>
    {
        ImmutableDictionary<TEnum, Func<TInternalMessage, IAsyncEnumerable<object>>> MessageFactoryMap { get; }
    }
}
