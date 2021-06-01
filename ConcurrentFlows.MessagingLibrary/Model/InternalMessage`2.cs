using System;

namespace ConcurrentFlows.MessagingLibrary.Model
{
    public abstract record InternalMessage<TEnum, TPayload>(TEnum Type, TPayload Payload)
        where TEnum : Enum
        where TPayload : class;
}
