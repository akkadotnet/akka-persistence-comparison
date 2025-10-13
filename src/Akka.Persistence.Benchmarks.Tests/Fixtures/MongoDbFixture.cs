using Akka.Hosting;
using Akka.Persistence.Hosting;
using Akka.Persistence.MongoDb.Hosting;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.MongoDb;
using static Akka.Persistence.Benchmarks.Fixtures.Consts;

namespace Akka.Persistence.Benchmarks.Fixtures;

public class MongoDbFixture: Fixture
{
    public MongoDbFixture() : this(false)
    {
    }
    
    public MongoDbFixture(bool useVolume)
    {
        var builder = new MongoDbBuilder()
            .WithUsername(Username)
            .WithPassword(Password)
            .WithReplicaSet(DatabaseName);

        if (useVolume)
            builder = builder.WithVolumeMount("benchmark-mongodb-data", "/data/db", AccessMode.ReadWrite);
        
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
        
        builder.WithMongoDbPersistence(
            connectionString: ConnectionStringFunc(),
            mode: PersistenceMode.Journal,
            autoInitialize: true);
    }
}