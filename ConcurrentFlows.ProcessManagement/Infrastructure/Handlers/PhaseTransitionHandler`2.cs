using ConcurrentFlows.ProcessManagement.Infrastructure.Dictionaries;
using ConcurrentFlows.ProcessManagement.Infrastructure.Messaging;
using ConcurrentFlows.ProcessManagement.Infrastructure.Records;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrentFlows.ProcessManagement.Infrastructure.Handlers
{
    public abstract class PhaseTransitionHandler<TMessage, TInput>
        : BackgroundService
        where TMessage : ProcessMessage<TInput>
        where TInput : ProcessInput
    {
        private readonly IWriterProvider writerProvider;
        private readonly IPhaseTransitions<TInput> phaseTransitions;
        private readonly IMessageFactory<TInput> messageFactory;
        private readonly IMessageSystemReader<TMessage> reader;

        private ConcurrentDictionary<Guid, ICollection<dynamic>> MessagesReceived = new ConcurrentDictionary<Guid, ICollection<dynamic>>();

        public PhaseTransitionHandler(
            IWriterProvider writerProvider,
            IPhaseTransitions<TInput> phaseTransitions,
            IMessageFactory<TInput> messageFactory,
            IMessageSystemReader<TMessage> reader)
        {
            this.writerProvider = writerProvider ?? throw new ArgumentNullException(nameof(writerProvider));
            this.phaseTransitions = phaseTransitions ?? throw new ArgumentNullException(nameof(phaseTransitions));
            this.messageFactory = messageFactory ?? throw new ArgumentNullException(nameof(messageFactory));
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var message in reader.ContinuousWaitAndReadAllAsync(stoppingToken))
            {
                if (MessagesReceived.TryGetValue(message.ProcessId, out var messages))
                    messages.Add(message);
                else
                    MessagesReceived[message.ProcessId] = new List<dynamic>() { message };

                var nextPhase = phaseTransitions[message.Phase](message.Phase, message.Input, MessagesReceived[message.ProcessId]);
                if (await nextPhase.SendIfEnding(writerProvider, () => EndedMessageFactory(message.ProcessId, nextPhase)))
                {
                    MessagesReceived.Remove(message.ProcessId, out var _);
                    await CompletedEvent(nextPhase, message);
                }
                else if (ShouldTransition(message.Phase, nextPhase))
                {
                    await PhaseTransitionEvent(nextPhase, message);
                    await foreach (var newMessage in messageFactory[nextPhase](message.ProcessId, nextPhase, message.Input, MessagesReceived[message.ProcessId]))
                    {
                        await writerProvider.RouteMessageByTypeAsync(newMessage);
                    }
                }
            }
        }

        protected abstract ProcessEndedMessage EndedMessageFactory(Guid processId, ProcessPhase phase);

        protected virtual ValueTask CompletedEvent(ProcessPhase phase, TMessage message)
            => ValueTask.CompletedTask;

        protected virtual ValueTask PhaseTransitionEvent(ProcessPhase phase, TMessage message)
            => ValueTask.CompletedTask;

        private bool ShouldTransition(ProcessPhase currentPhase, ProcessPhase newPhase)
            => !currentPhase.Equals(newPhase);
    }
}
