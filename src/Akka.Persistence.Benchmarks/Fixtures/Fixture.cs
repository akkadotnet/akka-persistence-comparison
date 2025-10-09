using Akka.Hosting;
using DotNet.Testcontainers.Containers;

namespace Akka.Persistence.Benchmarks.Fixtures;

public abstract class Fixture
{
    public abstract DockerContainer Container { get; }
    protected abstract Func<string> ConnectionStringFunc { get; }

    public string ConnectionString
    {
        get
        {
            if (Container.State == TestcontainersStates.Undefined)
            {
                Container.StartAsync().GetAwaiter().GetResult();
            }

            return ConnectionStringFunc();
        }
    }
    
    public abstract Task<bool> IsVolumeInitializedAsync(string persistenceId);

    public abstract void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider);
}