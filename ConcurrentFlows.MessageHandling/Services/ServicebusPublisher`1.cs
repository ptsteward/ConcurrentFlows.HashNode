using ConcurrentFlows.MessagingLibrary.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ConcurrentFlows.MessageHandling.Services
{
    public class ServicebusPublisher<TMessage> : IPublisher<TMessage> where TMessage : class
    {
        private readonly ISenderClient senderClient;

        public ServicebusPublisher(ISenderClient senderClient)
            => this.senderClient = senderClient ?? throw new ArgumentNullException(nameof(senderClient));

        public Task PublishAsync(TMessage message)
            => senderClient.SendAsync(ToMessage(message));

        private static Message ToMessage(TMessage message)
            => new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                Body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message))
            };
    }
}
