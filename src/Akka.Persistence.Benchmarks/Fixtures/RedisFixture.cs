using System;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Persistence.Redis;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.Redis;

namespace Akka.Persistence.Benchmarks.Fixtures;

public class RedisFixture: Fixture
{
    public RedisFixture(bool useVolume)
    {
        var builder = new RedisBuilder();

        if (useVolume)
            builder = builder.WithVolumeMount("benchmark-redis-data", "/data", AccessMode.ReadWrite);
        
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
                  plugin = "akka.persistence.journal.redis"
                  redis
                  {
                      configuration-string = "{{ConnectionStringFunc()}}"
                      database = 1
                  }
              }
              """)
        .WithFallback(RedisPersistence.DefaultConfig());
}