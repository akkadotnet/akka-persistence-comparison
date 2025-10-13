using Akka.Hosting;
using Akka.Persistence.Hosting;
using Akka.Persistence.Sql.Hosting;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using LinqToDB;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using static Akka.Persistence.Benchmarks.Fixtures.Consts;

namespace Akka.Persistence.Benchmarks.Fixtures;

public class SqlServerFixture: Fixture
{
    public SqlServerFixture() : this(false)
    {
    }
    
    public SqlServerFixture(bool useVolume)
    {
        var builder = new MsSqlBuilder();

        if (useVolume)
            builder = builder.WithVolumeMount("benchmark-sqlserver-data", "/var/opt/mssql", AccessMode.ReadWrite);
        
        var container = builder.Build();
        
        Container = container;
        ConnectionStringFunc = container.GetConnectionString;
    }
    
    public override DockerContainer Container { get; }
    protected override Func<string> ConnectionStringFunc { get; }
    
    public override async Task<bool> IsVolumeInitializedAsync(string persistenceId)
    {
        var connectionString = ConnectionStringFunc();
        
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand(
            """
                DECLARE @SearchString NVARCHAR(200) = @SearchValue;
                SELECT CASE 
                           WHEN EXISTS (
                               SELECT 1 
                               FROM dbo.journal
                               WHERE persistence_id = @SearchString
                           )
                           THEN CAST(1 AS BIT)
                           ELSE CAST(0 AS BIT)
                       END;
            """, conn);
        cmd.Parameters.AddWithValue("@SearchValue", persistenceId);

        try
        {
            return (bool)cmd.ExecuteScalar();
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
            providerName: ProviderName.SqlServer2022,
            mode: PersistenceMode.Journal,
            autoInitialize: true);
    }
}