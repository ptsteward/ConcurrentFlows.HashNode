using NATS.Client.Core;

namespace NATSUnleashed.MyNatsApp.Processors;

public interface IMessageProcessor
{
    Task ProcessMessageAsync(NatsMsg<ExampleMessage> msg, CancellationToken cancelToken);
}
