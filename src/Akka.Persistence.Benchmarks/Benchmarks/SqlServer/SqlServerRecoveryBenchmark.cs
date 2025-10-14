using System;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Persistence.Benchmarks.Configs;
using Akka.Persistence.Benchmarks.Fixtures;
using BenchmarkDotNet.Attributes;

namespace Akka.Persistence.Benchmarks.Benchmarks.SqlServer;

[Config(typeof(MacroBenchmarkConfig))]
public class SqlServerRecoveryBenchmark: RecoveryBenchmark
{
    private SqlServerFixture? _fixture;

    protected override Config PersistenceConfig => _fixture is null
        ? throw new Exception("Fixture not initialized") 
        : _fixture.Configuration;
    
    protected override Fixture Fixture => _fixture ?? throw new Exception("Fixture not initialized");
    
    protected override TimeSpan CompletionTimeout =>  TimeSpan.FromSeconds(120);
    
    protected override async Task SetupFixtureAsync(bool useVolume)
    {
        _fixture = new SqlServerFixture(useVolume);
        await _fixture.StartAsync();
    }

}