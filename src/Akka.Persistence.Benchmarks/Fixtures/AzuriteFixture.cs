using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.Azurite;

namespace Akka.Persistence.Benchmarks.Fixtures;

public class AzuriteFixture: IFixture
{
    public AzuriteFixture(bool useVolume = true)
    {
        var builder = new AzuriteBuilder();

        if (useVolume)
            builder.WithVolumeMount("benchmark-azurite-data", "/data", AccessMode.ReadWrite);
        
        var container = builder.Build();

        Container = container;
        ConnectionString = container.GetConnectionString();
    }
    
    public DockerContainer Container { get; }
    public string ConnectionString { get; }
    
    public Task InitializeAsync()
    {
        return Container.StartAsync();
    }
}