using ConcurrentFlows.MessageMultiplexing.Interfaces;
using ConcurrentFlows.MessageMultiplexing.Messages;
using ConcurrentFlows.MessageMultiplexing.Model;
using ConcurrentFlows.MessageMultiplexing.Model.Messages.External;
using ConcurrentFlows.MessageMultiplexing.Model.Messages.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ConcurrentFlows.MessageMultiplexing.Services
{
    public class SampleHubMessageFactory : IMessageFactory<SampleHubMessageType, SampleEntity, SampleHubInternalMessage>
    {
        private readonly IMetadataRepository metadataRepository;

        public SampleHubMessageFactory(IMetadataRepository metadataRepository)
        {
            this.metadataRepository = metadataRepository ?? throw new ArgumentNullException(nameof(metadataRepository));

            MessageFactoryMap = new Dictionary<SampleHubMessageType, Func<SampleHubInternalMessage, IAsyncEnumerable<object>>>()
        {
            { SampleHubMessageType.Created, msg => GetCreatedMessage(msg) },
            { SampleHubMessageType.Updated, msg => GetUpdatedMessage(msg) },
            { SampleHubMessageType.Deleted, msg => GetDeletedMessage(msg) }
        }.ToImmutableDictionary();
        }

        public ImmutableDictionary<SampleHubMessageType, Func<SampleHubInternalMessage, IAsyncEnumerable<object>>> MessageFactoryMap { get; }

        public async IAsyncEnumerable<EntityCreatedMessage> GetCreatedMessage(InternalMessage<SampleHubMessageType, SampleEntity> internalMessage)
        {
            var metadata = await metadataRepository.GetMetadadataAsync(internalMessage.Payload.Id);
            yield return new EntityCreatedMessage(internalMessage.Payload, metadata);
        }

        public async IAsyncEnumerable<EntityUpdatedMessage> GetUpdatedMessage(InternalMessage<SampleHubMessageType, SampleEntity> internalMessage)
        {
            var metadata = await metadataRepository.GetMetadadataAsync(internalMessage.Payload.Id);
            yield return new EntityUpdatedMessage(internalMessage.Payload, metadata);
        }

        public IAsyncEnumerable<EntityDeletedMessage> GetDeletedMessage(InternalMessage<SampleHubMessageType, SampleEntity> internalMessage)
            => new[] { new EntityDeletedMessage(internalMessage.Payload.Id) }.ToAsyncEnumerable();
    }
}
