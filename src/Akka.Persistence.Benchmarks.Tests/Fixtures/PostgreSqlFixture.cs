using Akka.Hosting;
using Akka.Persistence.Hosting;
using Akka.Persistence.Sql.Hosting;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using LinqToDB;
using Npgsql;
using Testcontainers.PostgreSql;
using static Akka.Persistence.Benchmarks.Fixtures.Consts;

namespace Akka.Persistence.Benchmarks.Fixtures;

public class PostgreSqlFixture: Fixture
{
    public PostgreSqlFixture() : this(false)
    {
    }
    
    public PostgreSqlFixture(bool useVolume)
    {
        var builder = new PostgreSqlBuilder()
            .WithUsername(Username)
            .WithPassword(Password)
            .WithDatabase(DatabaseName);

        if (useVolume)
            builder = builder.WithVolumeMount("benchmark-postgresql-data", "/var/lib/postgresql/data", AccessMode.ReadWrite);
        
        var container = builder.Build();
        
        Container = container;
        ConnectionStringFunc = container.GetConnectionString;
    }
    
    public override DockerContainer Container { get; }
    protected override Func<string> ConnectionStringFunc { get; }

    public override async Task<bool> IsVolumeInitializedAsync(string persistenceId)
    {
        var connectionString = ConnectionStringFunc();
        
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            """
            SELECT EXISTS (
                SELECT 1 
                FROM public.journal
                WHERE persistence_id = @SearchValue
            );
            """, conn);
        cmd.Parameters.AddWithValue("@SearchValue", persistenceId);

        try
        {
            return (bool)(cmd.ExecuteScalar() ?? false);
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
            providerName: ProviderName.PostgreSQL95,
            mode: PersistenceMode.Journal,
            autoInitialize: true);
    }
}