using ConcurrentFlows.ProcessManagement.Infrastructure.Handlers;
using ConcurrentFlows.ProcessManagement.Infrastructure.Messaging;
using ConcurrentFlows.ProcessManagement.Infrastructure.Records;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrentFlows.ProcessManagement.SayHello
{
    public class SayHelloValidationHandler
        : CommandHandler<SayHelloProcessMessage<SayHelloValidation>>
    {
        private readonly IMessageSystemWriter<SayHelloProcessMessage<ProcessActivity>> successWriter;
        private readonly IMessageSystemWriter<SayHelloProcessMessage<ProcessActivity>> failureWriter;

        public SayHelloValidationHandler(
            IMessageSystemReader<SayHelloProcessMessage<SayHelloValidation>> reader,
            IMessageSystemWriter<SayHelloProcessMessage<ProcessActivity>> successWriter,
            IMessageSystemWriter<SayHelloProcessMessage<ProcessActivity>> failureWriter)
            : base(reader)
        {
            this.successWriter = successWriter ?? throw new ArgumentNullException(nameof(successWriter));
            this.failureWriter = failureWriter ?? throw new ArgumentNullException(nameof(failureWriter));
        }

        public override async ValueTask HandleAsync(SayHelloProcessMessage<SayHelloValidation> command, CancellationToken stoppingToken)
        {
            var input = command.Input.Name;
            if (input.Length > 10)
                await failureWriter.WriteAsync(
                    new SayHelloProcessMessage<ProcessActivity>(command.ProcessId, command.Phase, command.Input, new SayHelloValidationFailure()));
            else
                await successWriter.WriteAsync(
                    new SayHelloProcessMessage<ProcessActivity>(command.ProcessId, command.Phase, command.Input, new SayHelloValidationSuccess()));
        }
    }
}
