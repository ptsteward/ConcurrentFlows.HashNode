namespace NATSUnleashed.MyNatsApp.Behaviors;

public interface INatsServiceBehavior
{
    Task ExecuteAsync(CancellationToken cancelToken);
}
