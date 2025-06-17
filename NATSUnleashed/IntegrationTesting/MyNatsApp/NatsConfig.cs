namespace NATSUnleashed.MyNatsApp;

public sealed record NatsConfig
{
    public string NatsUrl { get; set; } = default!;
    public string AppType { get; set; } = default!;
    public int? MaxMessages { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string? QueueGroup { get; set; } = default!;

    public void Deconstruct(
        out string natsUrl,
        out string appType,
        out int? maxMessages,
        out string subject,
        out string? queueGroup)
        => (natsUrl, appType, maxMessages, subject, queueGroup) = (NatsUrl, AppType, MaxMessages, Subject, QueueGroup);
}
