namespace NATSUnleashed.MyNatsApp.IntegrationTests.MyNatsApp.Services;

public sealed class NatsService_PubSubTests(
    NatsServiceFixture fixture)
    : IClassFixture<NatsServiceFixture>
{
    private readonly NatsServiceFixture _fixture = fixture;

    [Fact]
    public async Task PubSub_MessagesPublished_AreReceived()
    {
        var completion = new TaskCompletionSource();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var registration = completion.RegisterTaskCancelCompletion(timeout);

        var expectedCount = 100;
        var subject = _fixture.NatsTestSubject;

        var publisher = _fixture.BuildNatsService(
            AppBuilding.Publisher,
            expectedCount,
            subject);

        var processor = _fixture.BuildMockProcessor(completion, expectedCount);
        var subscriber = _fixture.BuildNatsService(
            AppBuilding.Subscriber,
            expectedCount,
            subject,
            processor);

        await subscriber.StartAsync(timeout.Token);
        await publisher.StartAsync(timeout.Token);
        await completion;
        await publisher.StopAsync(timeout.Token);
        await subscriber.StopAsync(timeout.Token);

        var received = processor.Received;

        Assert.False(timeout.IsCancellationRequested, $"Timeout Expired: {expectedCount}:{received?.Length}");
        Assert.Equal(expectedCount, received?.Length);
    }
}
