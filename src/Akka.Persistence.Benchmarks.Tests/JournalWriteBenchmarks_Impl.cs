using Akka.Persistence.Benchmarks.Fixtures;
using Xunit.Abstractions;

namespace Akka.Persistence.Benchmarks;

// derived benchmark classes use official Akka.Persistence hosting extension methods for plugin config

public class SqlServerJournalWriteBenchmarks : JournalWriteBenchmarks
{
    public SqlServerJournalWriteBenchmarks(ITestOutputHelper output)
        : base(nameof(SqlServerJournalWriteBenchmarks), output, timeoutDurationSeconds: 120)
    {
        Fixture = new SqlServerFixture(false);
    }

    protected override Fixture Fixture { get; }
}

public class PostgreSqlJournalWriteBenchmarks : JournalWriteBenchmarks
{
    public PostgreSqlJournalWriteBenchmarks(ITestOutputHelper output)
        : base(nameof(PostgreSqlJournalWriteBenchmarks), output)
    {
        Fixture = new PostgreSqlFixture(false);
    }
    
    protected override Fixture Fixture { get; }
}

public class MySqlJournalWriteBenchmarks : JournalWriteBenchmarks
{
    public MySqlJournalWriteBenchmarks(ITestOutputHelper output)
        : base(nameof(MySqlJournalWriteBenchmarks), output)
    {
        Fixture = new MySqlFixture(false);
    }
    
    protected override Fixture Fixture { get; }
}

public class MongoDbJournalWriteBenchmarks : JournalWriteBenchmarks
{
    public MongoDbJournalWriteBenchmarks(ITestOutputHelper output)
        : base(nameof(MongoDbJournalWriteBenchmarks), output)
    {
        Fixture = new MongoDbFixture(false);
    }
    
    protected override Fixture Fixture { get; }
}

public class RedisJournalWriteBenchmarks : JournalWriteBenchmarks
{
    public RedisJournalWriteBenchmarks(ITestOutputHelper output)
        : base(nameof(RedisJournalWriteBenchmarks), output)
    {
        Fixture = new RedisFixture(false);
    }
    
    protected override Fixture Fixture { get; }
}

public class AzureTableJournalWriteBenchmarks : JournalWriteBenchmarks
{
    public AzureTableJournalWriteBenchmarks(ITestOutputHelper output)
        : base(nameof(AzureTableJournalWriteBenchmarks), output)
    {
        Fixture = new AzuriteFixture(false);
    }
    
    protected override Fixture Fixture { get; }
}
