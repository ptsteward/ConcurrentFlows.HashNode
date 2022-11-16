namespace ConcurrentFlows.AsyncMediator2.MsgChannels.Broadcasting;

#if DEBUG
public
#else
internal
#endif
record struct SinkLink(Guid LinkId) : IDisposable
{
    private Action<Guid>? unlink = default;
    private bool disposed = false;

    public SinkLink(Guid linkId, Action<Guid> unlink)
        : this(linkId)
        => this.unlink = unlink;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing && !disposed)
            unlink?.Invoke(LinkId);
        disposed = true;
        unlink = null;
    }
}
