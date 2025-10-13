using Akka.Hosting;
using Akka.Persistence.Azure.Hosting;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.Azurite;

namespace Akka.Persistence.Benchmarks.Fixtures;

public class AzuriteFixture: Fixture
{
    public AzuriteFixture(): this(false)
    {
    }
    
    public AzuriteFixture(bool useVolume)
    {
        var builder = new AzuriteBuilder();

        if (useVolume)
            builder = builder.WithVolumeMount("benchmark-azurite-data", "/data", AccessMode.ReadWrite);
        
        var container = builder.Build();

        Container = container;
        ConnectionStringFunc = container.GetConnectionString;
    }
    
    public override DockerContainer Container { get; }
    protected override Func<string> ConnectionStringFunc { get; }

    public override Task<bool> IsVolumeInitializedAsync(string persistenceId)
    {
        throw new NotImplementedException();
    }

    public override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        if (Container.State == TestcontainersStates.Undefined)
            Container.StartAsync().GetAwaiter().GetResult();
        
        builder.WithAzurePersistence(
            connectionString: ConnectionStringFunc());
    }
}