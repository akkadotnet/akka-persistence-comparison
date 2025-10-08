using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.Redis;

namespace Akka.Persistence.Benchmarks.Fixtures;

public class RedisFixture: IFixture
{
    public RedisFixture(bool useVolume = true)
    {
        var builder = new RedisBuilder();

        if (useVolume)
            builder.WithVolumeMount("benchmark-redis-data", "/data", AccessMode.ReadWrite);
        
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