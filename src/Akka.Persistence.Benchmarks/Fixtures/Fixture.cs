using System;
using System.Threading.Tasks;
using Akka.Configuration;
using DotNet.Testcontainers.Containers;

namespace Akka.Persistence.Benchmarks.Fixtures;

public abstract class Fixture: IAsyncDisposable
{
    public abstract DockerContainer Container { get; }
    protected abstract Func<string> ConnectionStringFunc { get; }

    public abstract Config Configuration { get; }

    public virtual async Task StartAsync()
    {
        await Container.StartAsync();
    }
    
    public virtual async ValueTask DisposeAsync()
    {
        await Container.DisposeAsync();
    }
}