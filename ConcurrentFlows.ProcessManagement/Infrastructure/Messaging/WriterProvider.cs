using System;

namespace ConcurrentFlows.ProcessManagement.Infrastructure.Messaging
{
    public class WriterProvider : IWriterProvider
    {
        private readonly IServiceProvider serviceProvider;

        public WriterProvider(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public dynamic RequestWriter(object message)
        {
            var writerType = typeof(IMessageSystemWriter<>).MakeGenericType(message.GetType());
            dynamic writer = serviceProvider.GetService(writerType);
            if (writer is null)
                throw new ArgumentException($"Message Writer not registered for {message.GetType().Name}");
            return writer;
        }
    }
}
