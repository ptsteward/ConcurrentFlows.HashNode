using NATS.Client.Core;

namespace NATSUnleashed.MyNatsApp.Processors;

internal sealed class NoOpProcessor
    : IMessageProcessor
{
    public Task ProcessMessageAsync(NatsMsg<ExampleMessage> msg, CancellationToken cancelToken)
        => Task.CompletedTask;
}
