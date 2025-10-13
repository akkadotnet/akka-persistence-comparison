using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Persistence.Benchmarks.Configs;
using Akka.Persistence.Benchmarks.Fixtures;
using BenchmarkDotNet.Attributes;

namespace Akka.Persistence.Benchmarks.Benchmarks;

[Config(typeof(MacroBenchmarkConfig))]
public class SqlServerGroupPersist: BenchmarkBase
{
    private const int GroupSize = 10;
    private const int TestMessageCount = 10_000;
    private const int TotalMessageCount = TestMessageCount * GroupSize;
    
    private Fixture? _fixture;
    private IActorRef[]? _persistenceActors;
    
    [Params(1, 100)]
    public override int BatchSize { get; set; }

    protected override TimeSpan CompletionTimeout =>  TimeSpan.FromSeconds(90);

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
        _persistenceActors = Enumerable.Range(1, GroupSize)
            .Select(idx => ActorSystem!.ActorOf(Props.Create(() => new BenchActor($"GroupPersistPid_{idx}", TestMessageCount, null, BatchSize))))
            .ToArray();
    
        await Task.WhenAll(_persistenceActors.Select(actor => actor.Ask<Done>(Start.Instance)));
    }

    [IterationSetup]
    public void IterationSetup()
    {
    }
    
    protected override void IterationCleanup()
    {
    }
    
    [Benchmark(OperationsPerInvoke = TotalMessageCount)]
    public async Task PersistBenchmark()
    {
        if (BatchSize == 1)
        {
            await Task.WhenAll(_persistenceActors!.Select(actor => actor.Ask<Done>(new Cmd(PersistenceMode.Persist, null), CompletionTimeout)));
        }
        else
        {
            await Task.WhenAll(_persistenceActors!.Select(actor => actor.Ask<Done>(new Cmd(PersistenceMode.BatchPersist, null), CompletionTimeout)));
        }
    }
    
    [Benchmark(OperationsPerInvoke = TotalMessageCount)]
    public async Task PersistAsyncBenchmark()
    {
        if (BatchSize == 1)
        {
            await Task.WhenAll(_persistenceActors!.Select(actor => actor.Ask<Done>(new Cmd(PersistenceMode.PersistAsync, null), CompletionTimeout)));
        }
        else
        {
            await Task.WhenAll(_persistenceActors!.Select(actor => actor.Ask<Done>(new Cmd(PersistenceMode.BatchPersistAsync, null), CompletionTimeout)));
        }
    }
}