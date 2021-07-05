using ConcurrentFlows.ProcessManagement.Infrastructure.Dictionaries;
using ConcurrentFlows.ProcessManagement.Infrastructure.Records;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConcurrentFlows.ProcessManagement.SayHello
{
    public class SayHelloMessageFactory : MessageFactory<SayHelloInput>
    {
        public SayHelloMessageFactory()
        {
            this[SayHelloPhases.Validation] = (processId, phase, input, messages) => ValidationPhaseMessages(processId, phase, input, messages);
            this[SayHelloPhases.SayHello] = (processId, phase, input, messages) => SayHelloPhaseMessages(processId, phase, input, messages);
        }

        public IAsyncEnumerable<object> ValidationPhaseMessages(Guid processId, ProcessPhase phase, SayHelloInput input, IEnumerable<dynamic> currentMessages)
            => new[]
            {
            new SayHelloProcessMessage<SayHelloValidation>(processId, phase, input, new SayHelloValidation(input))
            }.ToAsyncEnumerable();

        public IAsyncEnumerable<object> SayHelloPhaseMessages(Guid processId, ProcessPhase phase, SayHelloInput input, IEnumerable<dynamic> currentMessages)
            => new[]
            {
            new SayHelloProcessMessage<SayHelloResponseActivity>(processId, phase, input, new SayHelloResponseActivity(input.Name))
            }.ToAsyncEnumerable();
    }
}
