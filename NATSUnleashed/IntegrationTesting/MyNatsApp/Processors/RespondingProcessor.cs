using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace NATSUnleashed.MyNatsApp.Processors;

internal sealed class RespondingProcessor(
    ILogger<RespondingProcessor> logger)
    : IMessageProcessor
{
    private readonly ILogger<RespondingProcessor> _logger = logger;

    public async Task ProcessMessageAsync(NatsMsg<ExampleMessage> msg, CancellationToken cancelToken)
    {
        _logger.LogInformation("Req/Rep - Request [{Message}]", msg.Data);
        var message = Random.Shared.NextReplyMessage();
        await msg.ReplyAsync(message, cancellationToken: cancelToken);
    }
}
