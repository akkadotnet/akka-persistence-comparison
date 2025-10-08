using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.MongoDb;
using static Akka.Persistence.Benchmarks.Fixtures.Consts;

namespace Akka.Persistence.Benchmarks.Fixtures;

public class MongoDbFixture: IFixture
{
    public MongoDbFixture(bool useVolume = true)
    {
        var builder = new MongoDbBuilder()
            .WithUsername(Username)
            .WithPassword(Password)
            .WithReplicaSet(DatabaseName);

        if (useVolume)
            builder.WithVolumeMount("benchmark-mongodb-data", "/data/db", AccessMode.ReadWrite);
        
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