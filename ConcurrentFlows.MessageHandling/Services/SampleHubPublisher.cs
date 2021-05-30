using ConcurrentFlows.MessageHandling.Hubs;
using ConcurrentFlows.MessageHandling.Interfaces;
using ConcurrentFlows.MessageHandling.Messages;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace ConcurrentFlows.MessageHandling.Services
{
public class SampleHubPublisher : IPublisher<EventMessage>
{
    private readonly IHubContext<SampleHub, ISampleHubClient> hubContext;

    public SampleHubPublisher(IHubContext<SampleHub, ISampleHubClient> hubContext)
        => this.hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));

    public Task PublishAsync(EventMessage message)
        => hubContext.Clients.All.ClientEvent(message);
}
}
