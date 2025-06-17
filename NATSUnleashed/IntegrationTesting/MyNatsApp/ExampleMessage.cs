namespace NATSUnleashed.MyNatsApp;

public sealed record ExampleMessage(
    int Id,
    string Message);

public static class MessageExtensions
{
    public static ExampleMessage NextPubSubMessage(
        this Random random,
        string? message = default)
        => new(random.Next(100, 199), message ?? "Hello World");

    public static ExampleMessage NextRequestMessage(
        this Random random,
        string? message = default)
        => new(random.Next(200, 299), message ?? "Anyone Home?");

    public static ExampleMessage NextReplyMessage(
        this Random random,
        string? message = default)
        => new(random.Next(300, 399), message ?? "I'm home!");
}

