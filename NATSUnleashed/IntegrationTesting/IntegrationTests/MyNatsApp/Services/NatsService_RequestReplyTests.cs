namespace NATSUnleashed.MyNatsApp.IntegrationTests.MyNatsApp.Services;

public class NatsService_RequestReplyTests(
    NatsServiceFixture fixture)
    : IClassFixture<NatsServiceFixture>
{
    private readonly NatsServiceFixture _fixture = fixture;

    [Fact]
    public async Task RequestReply_AllRequests_ReceiveReply()
    {
        var completion = new TaskCompletionSource();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var registration = completion.RegisterTaskCancelCompletion(timeout);

        var expectedCount = 100;
        var subject = _fixture.NatsTestSubject;

        var processor = _fixture.BuildMockProcessor(completion, expectedCount);
        var requestor = _fixture.BuildNatsService(
            AppBuilding.Requestor,
            expectedCount,
            subject,
            processor);

        var responder = _fixture.BuildNatsService(
            AppBuilding.Responder,
            expectedCount,
            subject);

        await responder.StartAsync(timeout.Token);
        await requestor.StartAsync(timeout.Token);
        await completion;
        await requestor.StopAsync(timeout.Token);
        await responder.StopAsync(timeout.Token);

        var received = processor.Received;

        Assert.False(timeout.IsCancellationRequested, $"Timeout Expired: {expectedCount}:{received?.Length}");
        Assert.Equal(expectedCount, received?.Length);
    }
}
