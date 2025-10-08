using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;
using static Akka.Persistence.Benchmarks.Fixtures.Consts;

namespace Akka.Persistence.Benchmarks.Fixtures;

public class PostgreSqlFixture: IFixture
{
    public PostgreSqlFixture(bool useVolume = true)
    {
        var builder = new PostgreSqlBuilder()
            .WithUsername(Username)
            .WithPassword(Password)
            .WithDatabase(DatabaseName);

        if (useVolume)
            builder.WithVolumeMount("benchmark-postgresql-data", "/var/lib/postgresql/data", AccessMode.ReadWrite);
        
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