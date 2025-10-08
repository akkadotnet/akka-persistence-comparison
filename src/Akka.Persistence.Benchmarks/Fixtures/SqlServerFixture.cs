using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.MsSql;
using static Akka.Persistence.Benchmarks.Fixtures.Consts;

namespace Akka.Persistence.Benchmarks.Fixtures;

public class SqlServerFixture: IFixture
{
    public SqlServerFixture(bool useVolume = true)
    {
        var builder = new MsSqlBuilder()
            .WithPassword(Password);

        if (useVolume)
            builder
                .WithVolumeMount("benchmark-sqlserver-data", "/var/opt/mssql/data", AccessMode.ReadWrite)
                .WithVolumeMount("benchmark-sqlserver-log", "/var/opt/mssql/log", AccessMode.ReadWrite)
                .WithVolumeMount("benchmark-sqlserver-secrets", "/var/opt/mssql/secrets", AccessMode.ReadWrite);
        
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