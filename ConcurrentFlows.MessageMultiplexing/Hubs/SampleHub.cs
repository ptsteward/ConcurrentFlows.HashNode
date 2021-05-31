using ConcurrentFlows.MessageMultiplexing.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ConcurrentFlows.MessageMultiplexing.Hubs
{
    public class SampleHub : Hub<ISampleHubClient>
    {
    }
}
