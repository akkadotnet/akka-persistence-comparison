using Akka.Hosting;
using Akka.Persistence.Hosting;
using Akka.Persistence.Sql.Hosting;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using LinqToDB;
using MySql.Data.MySqlClient;
using Testcontainers.MySql;
using static Akka.Persistence.Benchmarks.Fixtures.Consts;

namespace Akka.Persistence.Benchmarks.Fixtures;

public class MySqlFixture: Fixture
{
    public MySqlFixture() : this(false)
    {
    }
    
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

    public override async Task<bool> IsVolumeInitializedAsync(string persistenceId)
    {
        var connectionString = ConnectionStringFunc();
        
        await using var conn = new MySqlConnection(connectionString);
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand(
           """
               SELECT 
                   CASE 
                       WHEN EXISTS (
                           SELECT 1
                           FROM journal
                           WHERE persistence_id = @SearchValue
                       ) 
                       THEN TRUE 
                       ELSE FALSE 
                   END;
           """, conn);
        cmd.Parameters.AddWithValue("@SearchValue", persistenceId);

        try
        {
            return Convert.ToBoolean(cmd.ExecuteScalar());
        }
        catch
        {
            return false;
        }
    }

    public override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        if (Container.State == TestcontainersStates.Undefined)
            Container.StartAsync().GetAwaiter().GetResult();
        
        builder.WithSqlPersistence(
            connectionString: ConnectionStringFunc(),
            providerName: ProviderName.MySql,
            mode: PersistenceMode.Journal,
            autoInitialize: true);
    }
}