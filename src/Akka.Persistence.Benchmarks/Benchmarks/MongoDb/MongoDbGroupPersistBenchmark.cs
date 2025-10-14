using System;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Persistence.Benchmarks.Configs;
using Akka.Persistence.Benchmarks.Fixtures;
using BenchmarkDotNet.Attributes;

namespace Akka.Persistence.Benchmarks.Benchmarks.MongoDb;

[Config(typeof(MacroBenchmarkConfig))]
public class MongoDbGroupPersistBenchmark: GroupPersistBenchmark
{
    private MongoDbFixture? _fixture;

    protected override Config PersistenceConfig => _fixture is null
        ? throw new Exception("Fixture not initialized") 
        : _fixture.Configuration;
    
    protected override Fixture Fixture => _fixture ?? throw new Exception("Fixture not initialized");
    
    protected override async Task SetupFixtureAsync(bool useVolume)
    {
        _fixture = new MongoDbFixture(useVolume);
        await _fixture.StartAsync();
    }
}