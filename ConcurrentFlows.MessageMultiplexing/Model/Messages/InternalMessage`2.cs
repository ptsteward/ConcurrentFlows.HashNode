namespace ConcurrentFlows.MessageMultiplexing.Messages
{
    public record InternalMessage<TEnum, TPayload>(TEnum Type, TPayload Payload);
}
