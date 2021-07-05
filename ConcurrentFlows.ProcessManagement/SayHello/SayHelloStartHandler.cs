using ConcurrentFlows.ProcessManagement.Infrastructure.Dictionaries;
using ConcurrentFlows.ProcessManagement.Infrastructure.Handlers;
using ConcurrentFlows.ProcessManagement.Infrastructure.Messaging;
using ConcurrentFlows.ProcessManagement.Infrastructure.Records;
using System;

namespace ConcurrentFlows.ProcessManagement.SayHello
{
    public class SayHelloStartHandler : ProcessStartHandler<SayHelloProcessStartMessage, SayHelloInput>
    {
        public SayHelloStartHandler(
            IWriterProvider writerProvider,
            IMessageFactory<SayHelloInput> messageFactory,
            IMessageSystemReader<SayHelloProcessStartMessage> startReader)
            : base(writerProvider, messageFactory, startReader, SayHelloPhases.Validation)
        {
        }

        protected override ProcessStartedMessage StartedMessageFactory(Guid processId)
            => new SayHelloProcessStartedMessage(processId);
    }
}
