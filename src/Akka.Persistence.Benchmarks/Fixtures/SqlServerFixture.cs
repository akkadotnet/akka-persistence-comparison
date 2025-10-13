using System;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Persistence.Sql;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using LinqToDB;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Akka.Persistence.Benchmarks.Fixtures;

public class SqlServerFixture: Fixture
{
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

    public override Config Configuration
        => ConfigurationFactory.ParseString(
                $$"""
                akka.persistence.journal
                {
                    plugin = "akka.persistence.journal.sql"
                    sql
                    {
                        connection-string = "{{ConnectionStringFunc()}}"
                        provider-name = {{ProviderName.SqlServer2022}}
                    }
                }
                """)
            .WithFallback(SqlPersistence.DefaultConfiguration);
}