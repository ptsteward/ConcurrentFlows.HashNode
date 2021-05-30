using ConcurrentFlows.MessageHandling.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ConcurrentFlows.MessageHandling.Hubs
{
    public class SampleHub : Hub<ISampleHubClient>
    {
    }
}
