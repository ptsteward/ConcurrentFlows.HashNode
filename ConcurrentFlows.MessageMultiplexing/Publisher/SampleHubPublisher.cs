using ConcurrentFlows.MessageMultiplexing.Hubs;
using ConcurrentFlows.MessageMultiplexing.Interfaces;
using ConcurrentFlows.MessageMultiplexing.Model.Messages.External;
using ConcurrentFlows.MessagingLibrary.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace ConcurrentFlows.MessageMultiplexing.Publisher
{
    public class SampleHubPublisher
        : IPublisher<EntityCreatedMessage>,
        IPublisher<EntityUpdatedMessage>,
        IPublisher<EntityDeletedMessage>
    {
        private readonly IHubContext<SampleHub, ISampleHubClient> hubContext;

        public SampleHubPublisher(IHubContext<SampleHub, ISampleHubClient> hubContext)
            => this.hubContext = hubContext ?? throw new ArgumentNullException(nameof(SampleHub));

        public Task PublishAsync(EntityCreatedMessage message)
            => hubContext.Clients.All.EntityCreated(message);

        public Task PublishAsync(EntityUpdatedMessage message)
            => hubContext.Clients.All.EntityUpdated(message);

        public Task PublishAsync(EntityDeletedMessage message)
            => hubContext.Clients.All.EntityDeleted(message);
    }
}
