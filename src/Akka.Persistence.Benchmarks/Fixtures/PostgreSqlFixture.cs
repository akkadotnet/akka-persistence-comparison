using System;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Persistence.Sql;
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
    public override Config Configuration
        => ConfigurationFactory.ParseString(
                $$"""
                  akka.persistence.journal
                  {
                      plugin = "akka.persistence.journal.sql"
                      sql
                      {
                          connection-string = "{{ConnectionStringFunc()}}"
                          provider-name = {{ProviderName.PostgreSQL95}}
                      }
                  }
                  """)
            .WithFallback(SqlPersistence.DefaultConfiguration);
}