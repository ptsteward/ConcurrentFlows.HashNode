namespace NATSUnleashed.CSharpIntro;

public static class Extensions
{
    public static ExampleMessage NextPubSubMessage(
        this Random random)
        => new(random.Next(100, 199), "Hello World");

    public static ExampleMessage NextRequestMessage(
        this Random random)
        => new(random.Next(200, 299), "Anyone Home?");

    public static ExampleMessage NextReplyMessage(
        this Random random)
        => new(random.Next(300, 399), "I'm home!");
}
