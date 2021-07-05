namespace ConcurrentFlows.ProcessManagement.Infrastructure.Messaging
{
    public interface IWriterProvider
    {
        public dynamic RequestWriter(object message);
    }
}
