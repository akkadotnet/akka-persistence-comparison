using System;
using System.Threading.Tasks;
using Akka.Actor;
using BenchmarkDotNet.Attributes;

namespace Akka.Persistence.Benchmarks.Benchmarks;

public abstract class PersistBenchmark: BenchmarkBase
{
    private const int TestMessageCount = 2_500;
    
    private IActorRef? _persistenceActor;
    
    [Params(1, 100)]
    public override int BatchSize { get; set; }
    
    protected override Task GlobalSetupAsync()
    {
        return Task.CompletedTask;
    }
    
    [IterationSetup]
    public void IterationSetup()
    {
        // Generate unique persistence ID for this iteration
        var pid = $"benchmark-{Guid.NewGuid()}";
        _persistenceActor = ActorSystem!.ActorOf(Props.Create(() => new BenchActor(pid, TestMessageCount, null, BatchSize)));
    
        _persistenceActor.Ask<Done>(Start.Instance, CompletionTimeout).Wait();
    }
    
    protected override void IterationCleanup()
    {
        _persistenceActor.GracefulStop(TimeSpan.FromSeconds(5));
    }
    
    [Benchmark(OperationsPerInvoke = TestMessageCount)]
    public async Task Persist()
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
    public async Task PersistAsync()
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