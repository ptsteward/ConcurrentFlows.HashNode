using NATS.Client.Core;
using NATSUnleashed.MyNatsApp.Processors;
using System.Collections.Concurrent;

namespace NATSUnleashed.MyNatsApp.IntegrationTests.Scaffolding;

public sealed class MockProcessor(
    IProgress<int> messageCounter)
    : IMessageProcessor
{
    public NatsMsg<ExampleMessage>[] Received => _bag.ToArray();

    private readonly IProgress<int> _messageCounter = messageCounter;
    private readonly ConcurrentBag<NatsMsg<ExampleMessage>> _bag = [];

    public Task ProcessMessageAsync(NatsMsg<ExampleMessage> msg, CancellationToken cancelToken)
    {
        _bag.Add(msg);
        var count = _bag.Count;
        _messageCounter.Report(count);
        TestContext.Current.TestOutputHelper?.WriteLine($"{nameof(MockProcessor)} Total: {count} Current: {msg.Data}");
        return Task.CompletedTask;
    }
}
