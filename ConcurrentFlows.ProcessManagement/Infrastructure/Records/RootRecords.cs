using System;

namespace ConcurrentFlows.ProcessManagement.Infrastructure.Records
{
    public record ProcessStartMessage<TInput>(TInput Input);

    public record ProcessStartedMessage(Guid ProcessId);

    public record ProcessEndedMessage(Guid ProcessId, ProcessPhase Phase);

    public record ProcessActivity;

    public record ProcessMessage<TInput>(Guid ProcessId, ProcessPhase Phase, TInput Input);

    public record ProcessMessage<TInput, TActivity>(Guid ProcessId, ProcessPhase Phase, TInput Input, TActivity Activity)
        : ProcessMessage<TInput>(ProcessId, Phase, Input)
        where TActivity : ProcessActivity
        where TInput : ProcessInput;

    public record ProcessInput;

    public record ProcessPhase(string Phase)
    {
        public static ProcessPhase Completed = new ProcessPhase(nameof(Completed));
        public static ProcessPhase Failed = new ProcessPhase(nameof(Failed));
    }
}
