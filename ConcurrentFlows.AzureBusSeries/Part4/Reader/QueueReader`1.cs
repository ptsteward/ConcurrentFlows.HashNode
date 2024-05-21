using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static System.Threading.CancellationTokenSource;

namespace ConcurrentFlows.AzureBusSeries.Part4.Reader;

public sealed class QueueReader<T>
    : BackgroundService,
    IAsyncDisposable
{
    private readonly ILogger<QueueReader<T>> logger;
    private readonly ServiceBusProcessor processor;
    private readonly IMessageHandler<T> handler;

    private CancellationTokenSource? stoppingCts;

    public QueueReader(
        ILogger<QueueReader<T>> logger,
        ServiceBusProcessor processor,
        IMessageHandler<T> handler)
    {
        this.logger = logger.ThrowIfNull();
        this.processor = processor.ThrowIfNull();
        this.handler = handler.ThrowIfNull();

        processor.ProcessMessageAsync += ProcessMessageAsync;
        processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingCts = CreateLinkedTokenSource(stoppingToken);
        await processor.StartProcessingAsync(CancellationToken.None);

        await stoppingToken.CompleteOnCancelAsync();

        stoppingCts.Cancel();
        await processor.StopProcessingAsync(CancellationToken.None);
    }

    private Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body;
        var obj = body.ToObjectFromJson<T>();
        var cts = CreateLinkedTokenSource(stoppingCts!.Token, args.CancellationToken);
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

    public async ValueTask DisposeAsync()
    {
        await processor.DisposeAsync();
        stoppingCts?.Dispose();
        base.Dispose();
    }
}
