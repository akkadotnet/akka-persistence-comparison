using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using BenchmarkDotNet.Attributes;

namespace Akka.Persistence.Benchmarks.Benchmarks;

public abstract class GroupPersistBenchmark: BenchmarkBase
{
    private const int GroupSize = 10;
    private const int TestMessageCount = 5_000;
    private const int TotalMessageCount = TestMessageCount * GroupSize;
    
    private IActorRef[]? _persistenceActors;
    
    [Params(1, 100)]
    public override int BatchSize { get; set; }

    protected override Task GlobalSetupAsync()
    {
        return Task.CompletedTask;
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _persistenceActors = Enumerable.Range(1, GroupSize)
            .Select(idx => ActorSystem!.ActorOf(Props.Create(() => new BenchActor($"GroupPersistPid_{idx}", TestMessageCount, null, BatchSize))))
            .ToArray();
    
        Task.WhenAll(_persistenceActors.Select(actor => actor.Ask<Done>(Start.Instance))).Wait();
    }
    
    protected override void IterationCleanup()
    {
        Task.WhenAll(_persistenceActors!.Select(actor => actor.GracefulStop(TimeSpan.FromSeconds(5)))).Wait();
    }
    
    [Benchmark(OperationsPerInvoke = TotalMessageCount)]
    public async Task Persist()
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
    public async Task PersistAsync()
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