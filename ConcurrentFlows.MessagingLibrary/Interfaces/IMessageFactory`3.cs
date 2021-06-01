using ConcurrentFlows.MessagingLibrary.Model;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ConcurrentFlows.MessagingLibrary.Interfaces
{
    public interface IMessageFactory<TEnum, TPayload, TInternalMessage>
        where TEnum : Enum
        where TPayload : class
        where TInternalMessage : InternalMessage<TEnum, TPayload>
    {
        ImmutableDictionary<TEnum, Func<TInternalMessage, IAsyncEnumerable<object>>> MessageFactoryMap { get; }
    }
}
