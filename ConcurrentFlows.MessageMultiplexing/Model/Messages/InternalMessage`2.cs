using System;

namespace ConcurrentFlows.MessageMultiplexing.Messages
{
    public abstract record InternalMessage<TEnum, TPayload>(TEnum Type, TPayload Payload)
        where TEnum : Enum
        where TPayload : class;
}
