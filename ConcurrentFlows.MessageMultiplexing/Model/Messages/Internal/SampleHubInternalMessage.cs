using ConcurrentFlows.MessagingLibrary.Model;

namespace ConcurrentFlows.MessageMultiplexing.Model.Messages.Internal
{
    public record SampleHubInternalMessage(SampleHubMessageType Type, SampleEntity Payload)
        : InternalMessage<SampleHubMessageType, SampleEntity>(Type, Payload);
}
