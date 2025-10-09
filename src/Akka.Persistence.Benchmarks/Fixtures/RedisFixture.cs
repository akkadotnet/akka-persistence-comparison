using Akka.Hosting;
using Akka.Persistence.Hosting;
using Akka.Persistence.Redis.Hosting;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.Redis;

namespace Akka.Persistence.Benchmarks.Fixtures;

public class RedisFixture: Fixture
{
    public RedisFixture(bool useVolume)
    {
        var builder = new RedisBuilder();

        if (useVolume)
            builder = builder.WithVolumeMount("benchmark-redis-data", "/data", AccessMode.ReadWrite);
        
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
        
        builder.WithRedisPersistence(
            configurationString: ConnectionStringFunc(),
            mode: PersistenceMode.Journal,
            autoInitialize: true);
    }
}