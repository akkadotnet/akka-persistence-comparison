using System;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Persistence.MongoDb;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Builders;
using Mongo2Go;
using static Akka.Persistence.Benchmarks.Fixtures.Consts;

namespace Akka.Persistence.Benchmarks.Fixtures;

public class MongoDbFixture: Fixture
{
    private readonly MongoDbRunner _runner;
    
    public MongoDbFixture(bool useVolume)
    {
        _runner = MongoDbRunner.Start(singleNodeReplSet: true);

        ConnectionStringFunc = () =>
        {
            var s = _runner.ConnectionString.Split('?');
            return s[0] + $"{DatabaseName}?" + s[1];
        };
    }
    
    public override DockerContainer Container => throw new NotImplementedException();
    
    protected override Func<string> ConnectionStringFunc { get; }
    
    public override Config Configuration
    {
        get
        {
            var cs = ConnectionStringFunc();
            Console.WriteLine(cs);
            var s = cs.Split('?');
            var connectionString = s[0] + DatabaseName + "?directConnection=true&replicaSet=singleNodeReplSet&readPreference=primary";
            Console.WriteLine(connectionString);

            return ConfigurationFactory.ParseString(
                    $$"""
                      akka.persistence.journal
                      {
                          plugin = "akka.persistence.journal.mongodb"
                          mongodb
                          {
                              connection-string = "{{connectionString}}"
                          }
                      }
                      """)
                .WithFallback(MongoDbPersistence.DefaultConfiguration());
        }
    } 
    
    public override Task StartAsync()
    {
        // no-op
        return Task.CompletedTask;
    }

    public override ValueTask DisposeAsync()
    {
        _runner.Dispose();
        return ValueTask.CompletedTask;
    }
}
