using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATSUnleashed.MyNatsApp.Processors;

namespace NATSUnleashed.MyNatsApp.IntegrationTests.Scaffolding.Fixtures;

public sealed class NatsServiceFixture
{
    private readonly string[] AppTypes = [AppBuilding.Publisher, AppBuilding.Subscriber, AppBuilding.Requestor, AppBuilding.Responder];

    public string NatsTestSubject
        => $"{TestContext.Current.TestClassName()}.{TestContext.Current.TestMethodName($"{Guid.NewGuid()}")}";

    public MockProcessor BuildMockProcessor(
        TaskCompletionSource completion,
        int expectedCount)
        => new(completion.ToProgressCounter(expectedCount));

    public IHostedService BuildNatsService(
        string appType,
        int? maxMessages,
        string subject,
        IMessageProcessor? processor = default)
    {
        Assert.Contains(appType, AppTypes);
        var config = BuildConfig(appType, maxMessages, subject);
        var services = AddAppServices(config);
        var provider = BuildProvider(services, processor);
        return provider.GetRequiredService<IHostedService>();
    }

    private static IConfiguration BuildConfig(
        string appType,
        int? maxMessages,
        string subject)
    {
        var configDictionary = new Dictionary<string, string?>()
        {
            ["app:type"] = appType,
            ["app:maxMessages"] = $"{maxMessages}",
            ["nats:subject"] = subject
        };
        return new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddInMemoryCollection(configDictionary)
            .Build();
    }

    private static IServiceCollection AddAppServices(
        IConfiguration config)
        => new ServiceCollection()
        .AddAppServices(config)
        .AddLogging();

    private static IServiceProvider BuildProvider(
        IServiceCollection services,
        IMessageProcessor? processor = default)
        => processor switch
        {
            null => services.BuildServiceProvider(),
            var p => services.AddSingleton(p).BuildServiceProvider()
        };
}
