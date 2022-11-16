using ConcurrentFlows.AsyncMediator2.MsgChannels.Broadcasting;

namespace Concurrentflows.AsyncMediator.Tests;

public class SinkLinkTests
{
    [Theory, AutoMoqData]
    public void SinkLink_SameId_SameAction_Equal(
         Action<Guid> unlink,
         Guid id)
    {
        var link1 = new SinkLink(id, unlink);
        var link2 = new SinkLink(id, unlink);

        Assert.Equal(link1, link2);
    }

    [Theory, AutoMoqData]
    public void SinkLink_SameId_SameAction_OneDispose_NotEqual(
        Action<Guid> unlink,
        Guid id)
    {
        var link1 = new SinkLink(id, unlink);
        var link2 = new SinkLink(id, unlink);

        link1.Dispose();

        Assert.NotEqual(link1, link2);
    }

    [Theory, AutoMoqData]
    public void SinkLink_Dispose_Unlinks(
        [Frozen] Mock<Action<Guid>> mockUnlink,
        Guid id)
    {
        var link = new SinkLink(id, mockUnlink.Object);

        link.Dispose();

        mockUnlink.Verify(a => a.Invoke(id), Times.Once);
    }
}