namespace ConcurrentFlows.AzureBusSeries.Part4.AppModel;

public interface IMessageHandler<T>
{
    public Task HandleAsync(T message, CancellationToken cancelToken);
}
