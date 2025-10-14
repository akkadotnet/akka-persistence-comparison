using System;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Persistence.Azure;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.Azurite;

namespace Akka.Persistence.Benchmarks.Fixtures;

public class AzureFixture: Fixture
{
    public AzureFixture(bool useVolume)
    {
        var builder = new AzuriteBuilder();

        if (useVolume)
            builder = builder.WithVolumeMount("benchmark-azurite-data", "/data", AccessMode.ReadWrite);
        
        var container = builder.Build();

        Container = container;
        ConnectionStringFunc = container.GetConnectionString;
    }
    
    public override DockerContainer Container { get; }
    protected override Func<string> ConnectionStringFunc { get; }
    public override Config Configuration => ConfigurationFactory.ParseString(
            $$"""
              akka.persistence.journal
              {
                  plugin = "akka.persistence.journal.azure-table"
                  azure-table
                  {
                      connection-string = "{{ConnectionStringFunc()}}"
                  }
              }
              """)
        .WithFallback(AzurePersistence.DefaultConfig);
}