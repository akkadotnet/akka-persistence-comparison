using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Persistence.Benchmarks.Configs;
using Akka.Persistence.Benchmarks.Fixtures;
using BenchmarkDotNet.Attributes;

namespace Akka.Persistence.Benchmarks.Benchmarks;

[Config(typeof(MacroBenchmarkConfig))]
public class SqlServerPersist: BenchmarkBase
{
    private const int TestMessageCount = 10_000;
    
    private Fixture? _fixture;
    private IActorRef? _persistenceActor;
    
    [Params(1, 100)]
    public override int BatchSize { get; set; }
    
    protected override Config PersistenceConfig => _fixture is null
        ? throw new Exception("Fixture not initialized") 
        : _fixture.Configuration;
    
    protected override Fixture Fixture => _fixture ?? throw new Exception("Fixture not initialized");
    
    protected override async Task SetupFixtureAsync(bool useVolume)
    {
        _fixture = new SqlServerFixture(useVolume);
        await _fixture.StartAsync();
    }
    
    protected override async Task GlobalSetupAsync()
    {
        _persistenceActor =
            ActorSystem!.ActorOf(Props.Create(() => new BenchActor("SinglePersistPid", TestMessageCount, null, BatchSize)));
    
        await _persistenceActor.Ask<Done>(Start.Instance, CompletionTimeout);
    }

    [IterationSetup]
    public void IterationSetup()
    {
    }
    
    protected override void IterationCleanup()
    {
    }
    
    [Benchmark(OperationsPerInvoke = TestMessageCount)]
    public async Task PersistBenchmark()
    {
        if (BatchSize == 1)
        {
            await _persistenceActor.Ask<Done>(new Cmd(PersistenceMode.Persist, null), CompletionTimeout);
        }
        else
        {
            await _persistenceActor.Ask<Done>(new Cmd(PersistenceMode.BatchPersist, null), CompletionTimeout);
        }
    }
    
    [Benchmark(OperationsPerInvoke = TestMessageCount)]
    public async Task PersistAsyncBenchmark()
    {
        if (BatchSize == 1)
        {
            await _persistenceActor.Ask<Done>(new Cmd(PersistenceMode.PersistAsync, null), CompletionTimeout);
        }
        else
        {
            await _persistenceActor.Ask<Done>(new Cmd(PersistenceMode.BatchPersistAsync, null), CompletionTimeout);
        }
    }
}