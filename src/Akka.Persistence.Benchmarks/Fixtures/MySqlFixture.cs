using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.MySql;
using static Akka.Persistence.Benchmarks.Fixtures.Consts;

namespace Akka.Persistence.Benchmarks.Fixtures;

public class MySqlFixture: IFixture
{
    public MySqlFixture(bool useVolume = true)
    {
        var builder = new MySqlBuilder()
            .WithUsername(Username)
            .WithPassword(Password)
            .WithDatabase(DatabaseName);

        if (useVolume)
            builder.WithVolumeMount("benchmark-mysql-data", "/var/lib/mysql", AccessMode.ReadWrite);
        
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