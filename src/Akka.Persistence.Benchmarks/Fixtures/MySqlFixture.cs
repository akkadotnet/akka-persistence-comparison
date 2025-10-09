using Akka.Hosting;
using Akka.Persistence.Hosting;
using Akka.Persistence.Sql.Hosting;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using LinqToDB;
using Testcontainers.MySql;
using static Akka.Persistence.Benchmarks.Fixtures.Consts;

namespace Akka.Persistence.Benchmarks.Fixtures;

public class MySqlFixture: Fixture
{
    public MySqlFixture(bool useVolume)
    {
        var builder = new MySqlBuilder()
            .WithUsername(Username)
            .WithPassword(Password)
            .WithDatabase(DatabaseName);

        if (useVolume)
            builder = builder.WithVolumeMount("benchmark-mysql-data", "/var/lib/mysql", AccessMode.ReadWrite);
        
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
        
        builder.WithSqlPersistence(
            connectionString: ConnectionStringFunc(),
            providerName: ProviderName.MySql,
            schemaName: "akka",
            mode: PersistenceMode.Journal,
            autoInitialize: true);
    }
}