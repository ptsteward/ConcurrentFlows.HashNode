using ConcurrentFlows.ProcessManagement.Infrastructure.Records;
using System;

namespace ConcurrentFlows.ProcessManagement.SayHello
{
    public record SayHelloInput(string Name)
    : ProcessInput;

    public record SayHelloProcessStartMessage(SayHelloInput Input)
        : ProcessStartMessage<SayHelloInput>(Input);

    public record SayHelloProcessStartedMessage(Guid ProcessId)
        : ProcessStartedMessage(ProcessId);

    public record SayHelloEndedMessage(Guid ProcessId, ProcessPhase Phase)
            : ProcessEndedMessage(ProcessId, Phase);

    public record SayHelloProcessMessage<TActivity>(Guid ProcessId, ProcessPhase Phase, SayHelloInput Input, TActivity Activity)
        : ProcessMessage<SayHelloInput, TActivity>(ProcessId, Phase, Input, Activity)
        where TActivity : ProcessActivity;

    public record SayHelloValidation(SayHelloInput Input)
            : ProcessActivity;

    public record SayHelloValidationSuccess
        : ProcessActivity;

    public record SayHelloValidationFailure
        : ProcessActivity;

    public record SayHelloResponseActivity(string Input)
        : ProcessActivity;

    public record SayHelloCompletedActivity
        : ProcessActivity;

    public record SayHelloResponseMessage(string message);

    public record SayHelloPhases(string Phase)
            : ProcessPhase(Phase)
    {
        public static ProcessPhase Validation = new ProcessPhase(nameof(Validation));
        public static ProcessPhase SayHello = new ProcessPhase(nameof(SayHello));
    }
}
