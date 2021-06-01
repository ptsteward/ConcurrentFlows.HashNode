namespace ConcurrentFlows.MessageMultiplexing.Model.Messages.External
{
    public record EntityCreatedMessage(SampleEntity Entity, string Metadata);

    public record EntityUpdatedMessage(SampleEntity Entity, string Metadata);

    public record EntityDeletedMessage(int Id);
}
