using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream.Models;
using NATS.Client.JetStream;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var opts = new NatsOpts
{
    Url = "nats://leaf:leaf@leaf-node:4222",
    LoggerFactory = loggerFactory
};

await using var connection = new NatsConnection(opts);
var context = new NatsJSContext(connection);

var streamConfig = new StreamConfig("local-stream", ["leaf.>"]);
var stream = await context.CreateStreamAsync(streamConfig, cts.Token);
var consumer = await stream.CreateOrderedConsumerAsync(cancellationToken: cts.Token);

await context.PublishAsync<string>("leaf.data", "Hello World");

var msg = await consumer.NextAsync<string>(cancellationToken: cts.Token);
logger.LogInformation("Leaf received: {Message}", msg?.Data);
await (msg?.AckAsync(cancellationToken: cts.Token) ?? ValueTask.CompletedTask);

await connection.PublishAsync("remote.data", "Message to Remote", cancellationToken: cts.Token);
