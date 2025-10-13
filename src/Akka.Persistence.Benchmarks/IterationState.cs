using System;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Persistence.Benchmarks;

public sealed class IterationState: IDisposable
{
    public IterationState(TaskCompletionSource completionTcs, TaskCompletionSource startupTcs, IActorRef aggregator, IActorRef[] persistentActors)
    {
        CompletionTcs = completionTcs;
        StartupTcs = startupTcs;
        Aggregator = aggregator;
        PersistentActors = persistentActors;
    }

    public TaskCompletionSource CompletionTcs { get; }
    public TaskCompletionSource StartupTcs { get; }
    public IActorRef Aggregator { get; }
    public IActorRef[] PersistentActors { get; }

    public void Dispose()
    {
        StartupTcs.TrySetResult();
        CompletionTcs.TrySetResult();
        
        foreach (var actor in PersistentActors)
            actor.Tell(PoisonPill.Instance);
        
        Aggregator.Tell(PoisonPill.Instance);
    }
}