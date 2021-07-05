using ConcurrentFlows.ProcessManagement.Infrastructure.Handlers;
using ConcurrentFlows.ProcessManagement.Infrastructure.Messaging;
using ConcurrentFlows.ProcessManagement.Infrastructure.Records;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrentFlows.ProcessManagement.SayHello
{
    public class SayHelloResponseHandler
        : CommandHandler<SayHelloProcessMessage<SayHelloResponseActivity>>
    {
        private readonly IMessageSystemWriter<SayHelloResponseMessage> responseWriter;
        private readonly IMessageSystemWriter<SayHelloProcessMessage<ProcessActivity>> completedWriter;

        public SayHelloResponseHandler(
            IMessageSystemReader<SayHelloProcessMessage<SayHelloResponseActivity>> reader,
            IMessageSystemWriter<SayHelloResponseMessage> responseWriter,
            IMessageSystemWriter<SayHelloProcessMessage<ProcessActivity>> completedWriter)
            : base(reader)
        {
            this.responseWriter = responseWriter ?? throw new ArgumentNullException(nameof(responseWriter));
            this.completedWriter = completedWriter ?? throw new ArgumentNullException(nameof(completedWriter));
        }

        public override async ValueTask HandleAsync(SayHelloProcessMessage<SayHelloResponseActivity> command, CancellationToken stoppingToken)
        {
            var message = $"Hello there, {command.Input.Name}";
            await responseWriter.WriteAsync(new SayHelloResponseMessage(message));
            await completedWriter.WriteAsync(new SayHelloProcessMessage<ProcessActivity>(command.ProcessId, command.Phase, command.Input, new SayHelloCompletedActivity()));
        }
    }
}
