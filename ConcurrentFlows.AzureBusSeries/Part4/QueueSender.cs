using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static System.Environment;

namespace ConcurrentFlows.AzureBusSeries.Part4;

public sealed class QueueSender
    : BackgroundService,
    IAsyncDisposable
{
    private readonly ILogger<QueueSender> logger;
    private readonly ServiceBusSender sender;
    private readonly int count;

    public QueueSender(
        ILogger<QueueSender> logger,
        ServiceBusSender sender,
        int count = 5)

    {
        this.logger = logger.ThrowIfNull();
        this.sender = sender.ThrowIfNull();
        this.count = count;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeout.Token);
        try
        {
            foreach(var id in Enumerable.Range(0, count))
            {
                var notification = new Notification(id, "Hello World");
                var body = JsonSerializer.Serialize(notification);
                var message = new ServiceBusMessage(body);

                await sender.SendMessageAsync(message, stoppingToken);
                logger.LogInformation($"Sent Message:{NewLine}{body}");
            }
            logger.LogInformation($"Finished");
        }
        catch (OperationCanceledException ex)
            when (timeout.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Operation timed out");
            throw;
        }
        catch (OperationCanceledException ex)
            when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation(ex, "Shutdown early");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await sender.DisposeAsync();
        base.Dispose();
    }
}

public sealed record Notification(
    int Id, 
    string Content);

public interface IMessageHandler<T>
{
    public Task HandleAsync(T message, CancellationToken cancelToken);
}

public sealed class QueueReader<T>
    : IHostedService,
    IDisposable,
    IAsyncDisposable
{
    private readonly ILogger<QueueReader<T>> logger;
    private readonly ServiceBusProcessor processor;
    private readonly IMessageHandler<T> handler;

    private Task? execution;
    private CancellationTokenSource? stoppingCts;

    public Task? ExecuteTask => execution;

    public QueueReader(
        ILogger<QueueReader<T>> logger, 
        ServiceBusProcessor processor,
        IMessageHandler<T> handler)
    {
        this.logger = logger.ThrowIfNull();
        this.processor = processor.ThrowIfNull();
        this.handler = handler.ThrowIfNull();
    }

    public async Task StartAsync(CancellationToken cancelToken)
    {        
        stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        processor.ProcessMessageAsync += ProcessMessageAsync;
        processor.ProcessErrorAsync += ProcessErrorAsync;
        await processor.StartProcessingAsync(stoppingCts.Token);
        execution = ExecuteAsync(stoppingCts.Token);
    }

    private Task ExecuteAsync(CancellationToken stoppingToken)
    {        
        var tcs = new TaskCompletionSource();
        stoppingToken.Register(() => tcs.TrySetCanceled(stoppingToken));
        return tcs.Task;
    }

    private Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body;
        var obj = body.ToObjectFromJson<T>();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(args.CancellationToken, stoppingCts!.Token);
        return handler.HandleAsync(obj, cts.Token);
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogWarning("Error Processing {@Error}",
            new
            {
                args.Identifier,
                ErrorSource = $"{args.ErrorSource}",
                Exception = $"{args.Exception}"
            });
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken canceLToken)
    {
        if (execution is null)
            return;

        try
        {
            stoppingCts!.Cancel();
        }
        finally
        {            
            await Task.WhenAny(execution);
        }

    }

    public async ValueTask DisposeAsync()
    {
        await processor.DisposeAsync();
        stoppingCts?.Dispose();
    }

    public void Dispose()
    {
        stoppingCts?.Dispose();        
    }
}
