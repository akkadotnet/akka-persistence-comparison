using DotNet.Testcontainers.Containers;

namespace Akka.Persistence.Benchmarks.Fixtures;

public interface IFixture
{
    DockerContainer Container { get; }
    string ConnectionString { get; }
    Task InitializeAsync();
}