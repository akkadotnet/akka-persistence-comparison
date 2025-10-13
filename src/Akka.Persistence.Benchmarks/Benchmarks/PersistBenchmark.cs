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
    
    protected override async Task GlobalSetupAsync()
    {
        _persistenceActor =
            ActorSystem!.ActorOf(Props.Create(() => new BenchActor("SingleRecoveryPid", TestMessageCount, null, BatchSize)));
    
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