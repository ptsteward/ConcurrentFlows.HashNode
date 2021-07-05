using ConcurrentFlows.ProcessManagement.Infrastructure.Dictionaries;
using ConcurrentFlows.ProcessManagement.Infrastructure.Messaging;
using ConcurrentFlows.ProcessManagement.Infrastructure.Records;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrentFlows.ProcessManagement.Infrastructure.Handlers
{
    public abstract class ProcessStartHandler<TStartMessage, TInput>
        : BackgroundService
        where TStartMessage : ProcessStartMessage<TInput>
        where TInput : ProcessInput
    {
        private readonly IMessageSystemReader<TStartMessage> startReader;
        private readonly IWriterProvider writerProvider;
        private readonly IMessageFactory<TInput> messageFactory;
        private readonly ProcessPhase startPhase;

        public ProcessStartHandler(
            IWriterProvider writerProvider,
            IMessageFactory<TInput> messageFactory,
            IMessageSystemReader<TStartMessage> startReader,
            ProcessPhase startPhase)
        {
            this.startReader = startReader ?? throw new ArgumentNullException(nameof(startReader));
            this.writerProvider = writerProvider ?? throw new ArgumentNullException(nameof(writerProvider));
            this.messageFactory = messageFactory ?? throw new ArgumentNullException(nameof(messageFactory));
            this.startPhase = startPhase ?? throw new ArgumentNullException(nameof(startPhase));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var message in startReader.ContinuousWaitAndReadAllAsync(stoppingToken))
            {
                var processId = Guid.NewGuid();
                await SendStartedMessage(processId);
                await SendFirstPhaseMessages(processId, message);
            }
        }

        private async ValueTask SendStartedMessage(Guid processId)
        {

            var message = StartedMessageFactory(processId);
            await writerProvider.RouteMessageByTypeAsync(message);
        }

        private async ValueTask SendFirstPhaseMessages(Guid processId, TStartMessage message)
        {
            await foreach (var newMessage in messageFactory[startPhase](processId, startPhase, message.Input, Enumerable.Empty<object>()))
            {
                await writerProvider.RouteMessageByTypeAsync(newMessage);
            }

        }

        protected abstract ProcessStartedMessage StartedMessageFactory(Guid processId);
    }
}
