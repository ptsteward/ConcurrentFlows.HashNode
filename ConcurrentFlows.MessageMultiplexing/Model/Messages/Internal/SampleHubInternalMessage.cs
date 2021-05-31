using ConcurrentFlows.MessageMultiplexing.Messages;

namespace ConcurrentFlows.MessageMultiplexing.Model.Messages.Internal
{
    public record SampleHubInternalMessage(SampleHubMessageType Type, SampleEntity Payload)
        : InternalMessage<SampleHubMessageType, SampleEntity>(Type, Payload);
}
