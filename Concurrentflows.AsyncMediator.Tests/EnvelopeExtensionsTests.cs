//using ConcurrentFlows.AsyncMediator3;

//namespace Concurrentflows.AsyncMediator.Tests;

//public class EnvelopeExtensionsTests
//{
//    [Theory, AutoMoqData]
//    public void CommandEnvelope_IsDuplicated()
//    {
//        var payload = string.Empty;
//        var timeout = TimeSpan.FromSeconds(1);
//        var onComplete = () => Task.CompletedTask;
//        var onFailure = () => Task.CompletedTask;
//        var expected = payload.ToEnvelope(timeout, onComplete, onFailure);
        
//        var actual = expected.FromEnvelope(timeout, onComplete, onFailure);

//        actual.Should()
//            .BeEquivalentTo(expected, 
//                opts => opts.Excluding(e => e.MessageId));
//    }

//    [Theory, AutoMoqData]
//    public void QueryEnvelope_IsDuplicated(
//        string payload,
//        TimeSpan timeout,
//        Func<Task> onComplete,
//        Func<Task> onFailure)
//    {
//        var expected = payload.ToEnvelope<string, int>(timeout, onComplete, onFailure);

//        var actual = expected.FromEnvelope(timeout, onComplete, onFailure);

//        actual.Should()
//            .BeEquivalentTo(expected,
//                opts => opts.Excluding(e => e.MessageId));
//    }
//}
