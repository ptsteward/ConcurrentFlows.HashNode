using ConcurrentFlows.AsyncMediator3;

namespace Concurrentflows.AsyncMediator.Tests;

public class TypeTests
{
    [Fact]
    public void ClosedGeneric_AssignableTo_OpenGeneric()
    {
        var closedType = typeof(Envelope<string, int>);

        closedType.Should()
            .BeAssignableTo(typeof(Envelope<,>));
    }

    [Fact]
    public void OpenGeneric_NotAssignableTo_ClosedGeneric()
    {
        var openType = typeof(Envelope<,>);

        openType.Should()
            .NotBeAssignableTo(typeof(Envelope<string, int>));
    }
}
