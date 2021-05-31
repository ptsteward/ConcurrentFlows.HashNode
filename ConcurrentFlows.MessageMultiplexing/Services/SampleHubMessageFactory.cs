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
        public SampleHubMessageFactory()
        {
            MessageFactoryMap = new Dictionary<SampleHubMessageType, Func<SampleHubInternalMessage, IAsyncEnumerable<object>>>()
            {
                { SampleHubMessageType.Created, msg => GetCreatedMessage(msg) },
                { SampleHubMessageType.Updated, msg => GetUpdatedMessage(msg) },
                { SampleHubMessageType.Deleted, msg => GetDeletedMessage(msg) }
            }.ToImmutableDictionary();
        }

        public IAsyncEnumerable<EntityCreatedMessage> GetCreatedMessage(InternalMessage<SampleHubMessageType, SampleEntity> internalMessage)
            => new[] { new EntityCreatedMessage(internalMessage.Payload) }.ToAsyncEnumerable();

        public IAsyncEnumerable<EntityUpdatedMessage> GetUpdatedMessage(InternalMessage<SampleHubMessageType, SampleEntity> internalMessage)
            => new[] { new EntityUpdatedMessage(internalMessage.Payload) }.ToAsyncEnumerable();

        public IAsyncEnumerable<EntityDeletedMessage> GetDeletedMessage(InternalMessage<SampleHubMessageType, SampleEntity> internalMessage)
            => new[] { new EntityDeletedMessage(internalMessage.Payload.Id) }.ToAsyncEnumerable();

        public ImmutableDictionary<SampleHubMessageType, Func<SampleHubInternalMessage, IAsyncEnumerable<object>>> MessageFactoryMap { get; }
    }
}
