using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Persistence.Benchmarks.Fixtures;
using BenchmarkDotNet.Attributes;

namespace Akka.Persistence.Benchmarks;

public abstract class BenchmarkBase
{
    protected ActorSystem? ActorSystem { get; private set; }
    
    protected abstract Fixture? Fixture { get; }
    
    protected TaskCompletionSource? CompletionTaskSource { get; set; }

    protected Task CompletionTask => CompletionTaskSource?.Task ?? throw new Exception("CompletionTaskSource is null");

    public virtual int BatchSize { get; set; } = 50;
    
    /// <summary>
    /// Amount of time we're going to give to an individual benchmark iteration to complete
    /// </summary>
    protected virtual TimeSpan CompletionTimeout => TimeSpan.FromSeconds(30);
    
    protected abstract Config PersistenceConfig { get; }
    
    protected abstract Task SetupFixtureAsync(bool useVolume);
    
    [GlobalSetup]
    public virtual async Task SetupAsync()
    {
        Console.WriteLine("Setting up persistence read benchmark...");
        
        await SetupFixtureAsync(false);
        
        // Setup actor system
        var config = PersistenceConfig.WithFallback(
            """
            akka {
                log-config-on-start = off
                stdout-loglevel = INFO
                loglevel = INFO
                log-dead-letters = off # no dead letters
                actor {
                    debug {
                        receive = off
                        autoreceive = off
                        lifecycle = off
                        event-stream = off
                        unhandled = off
                    }
                }
            }
            """);

        ActorSystem = ActorSystem.Create("persistence-benchmark", config);

        await GlobalSetupAsync();
        
        Console.WriteLine("Persistence read benchmark setup complete.");
    }
    
    protected abstract Task GlobalSetupAsync();

    [GlobalCleanup]
    public virtual async Task CleanupAsync()
    {
        // Cleanup actor system
        if(ActorSystem is not null)
            await ActorSystem.Terminate();
        
        // Cleanup fixture
        if(Fixture is not null)
            await Fixture.DisposeAsync();
    }

    [IterationCleanup]
    public void IterationCleanupInternal()
    {
        if (CompletionTaskSource != null)
        {
            CompletionTask.Wait();
            CompletionTaskSource = null;
        }
        IterationCleanup();
    }

    protected abstract void IterationCleanup();
}