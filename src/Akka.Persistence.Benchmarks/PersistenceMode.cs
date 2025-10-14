namespace Akka.Persistence.Benchmarks;

public enum PersistenceMode
{
    None,
    Persist,
    PersistAsync,
    BatchPersist,
    BatchPersistAsync,
}