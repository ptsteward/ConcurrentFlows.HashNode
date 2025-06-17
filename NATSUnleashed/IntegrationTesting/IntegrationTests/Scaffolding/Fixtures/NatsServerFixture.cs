using Testcontainers.Nats;

[assembly: AssemblyFixture(typeof(NatsServerFixture))]

namespace NATSUnleashed.MyNatsApp.IntegrationTests.Scaffolding.Fixtures;

public sealed class NatsServerFixture
    : IAsyncLifetime
{
    public string NatsUrlkey = "nats__url";

    private readonly NatsContainer _nats;

    public NatsServerFixture()
    {
        _nats = new NatsBuilder()
            .WithName(nameof(NatsServerFixture))
            .WithImage("nats:latest")
            .Build();
    }

    public async ValueTask InitializeAsync()
    {
        await _nats.StartAsync();
        Environment.SetEnvironmentVariable(NatsUrlkey, _nats.GetConnectionString());
    }

    public async ValueTask DisposeAsync()
    {
        await _nats.DisposeAsync();
        Environment.SetEnvironmentVariable(NatsUrlkey, string.Empty);
    }
}
