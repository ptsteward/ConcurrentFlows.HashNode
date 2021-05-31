using ConcurrentFlows.MessageMultiplexing.Model;

namespace ConcurrentFlows.MessageMultiplexing.Model.Messages.External
{
    public record EntityCreatedMessage(SampleEntity Entity);

    public record EntityUpdatedMessage(SampleEntity Entity);

    public record EntityDeletedMessage(int Id);
}
