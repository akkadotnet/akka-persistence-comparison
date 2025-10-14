using System;
using System.Threading.Tasks;
using Akka.Actor;
using BenchmarkDotNet.Attributes;

namespace Akka.Persistence.Benchmarks.Benchmarks;

public abstract class RecoveryBenchmark: BenchmarkBase
{
    private const int TestMessageCount = 2_000;
    
    private IActorRef? _persistentActor;
    private IActorRef? _watchActor;
    private string? _persistenceId;
    
    public override int BatchSize { get; set; } = 500;
    
    protected override Task GlobalSetupAsync()
    {
        return Task.CompletedTask;
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Generate unique persistence ID for this iteration
        _persistenceId = $"benchmark-{Guid.NewGuid()}";

        // Pre-populate the journal with events
        var prepActor = ActorSystem!.ActorOf(Props.Create(() => new BenchActor(_persistenceId, TestMessageCount, null, BatchSize)));
        prepActor.Ask<Done>(new Cmd(PersistenceMode.BatchPersist, null)).Wait(CompletionTimeout);
        prepActor.GracefulStop(TimeSpan.FromSeconds(5)).Wait();

        // Create the actor that will recover (but don't wait for recovery yet)
        CompletionTaskSource = new TaskCompletionSource();
        var startupTcs = new TaskCompletionSource();
        _watchActor = ActorSystem.ActorOf(Props.Create(() => new AggregatorActor(CompletionTaskSource, startupTcs, 1)));
        startupTcs.Task.Wait();
    }
    
    protected override void IterationCleanup()
    {
        CompletionTaskSource = null;
        Task.WhenAll(
                _watchActor.GracefulStop(TimeSpan.FromSeconds(5)), 
                _persistentActor.GracefulStop(TimeSpan.FromSeconds(5)))
            .Wait();
        _watchActor = null;
        _persistentActor = null;
    }
    
    [Benchmark(OperationsPerInvoke = TestMessageCount)]
    public async Task RecoveryBenchmarkMethod()
    {
        _persistentActor = ActorSystem!.ActorOf(Props.Create(() => new BenchActor(_persistenceId!, TestMessageCount, _watchActor, 1)));
        await CompletionTask;
    }
}