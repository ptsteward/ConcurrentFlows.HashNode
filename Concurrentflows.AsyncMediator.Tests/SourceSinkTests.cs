using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurrentflows.AsyncMediator.Tests;

public class SourceSinkTests
{
[Fact]
public async Task Source_BroadcastsTo_AllConsumers()
{
    var provider = new ServiceCollection()
        .AddMsgChannel<Message>()
        .AddTransient<Originator>()
        .AddTransient<Consumer>()
        .BuildServiceProvider();

    var originator1 = provider.GetRequiredService<Originator>();
    var originator2 = provider.GetRequiredService<Originator>();
    var consumer1 = provider.GetRequiredService<Consumer>();
    var consumer2 = provider.GetRequiredService<Consumer>();
    originator1.Should().NotBe(originator2);
    consumer1.Should().NotBe(consumer2);        
    
    var count = 100;
    using var cts = new CancellationTokenSource();
    var consume1 = consumer1.CollectAllAsync(cts.Token);
    var consume2 = consumer2.CollectAllAsync(cts.Token);
    var sending1 = originator1.ProduceManyAsync(count, cts.Token);
    var sending2 = originator2.ProduceManyAsync(count, cts.Token);
    
    await Task.WhenAll(sending1, sending2);
    cts.Cancel();
    var set1 = await consume1;
    var set2 = await consume2;

    set1.Should().HaveCount(count * 2);
    set2.Should().HaveCount(count * 2);
    set1.Should().BeEquivalentTo(set2);
}
}
