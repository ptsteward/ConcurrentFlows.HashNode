using ConcurrentFlows.ProcessManagement.Infrastructure.Messaging;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrentFlows.ProcessManagement.Infrastructure.Handlers
{
    public abstract class CommandHandler<TMesssage> : BackgroundService
    {
        protected readonly IMessageSystemReader<TMesssage> reader;

        public CommandHandler(IMessageSystemReader<TMesssage> reader)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var command in reader.ContinuousWaitAndReadAllAsync(stoppingToken))
            {
                await HandleAsync(command, stoppingToken);
            }
        }

        public abstract ValueTask HandleAsync(TMesssage command, CancellationToken stoppingToken);
    }
}
