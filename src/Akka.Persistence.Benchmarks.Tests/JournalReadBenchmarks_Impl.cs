using Akka.Persistence.Benchmarks.Fixtures;
using Xunit.Abstractions;

namespace Akka.Persistence.Benchmarks;

// derived benchmark classes use official Akka.Persistence hosting extension methods for plugin config

public class SqlServerJournalReadBenchmarks : JournalReadBenchmarks
{
    public SqlServerJournalReadBenchmarks(ITestOutputHelper output)
        : base(nameof(SqlServerJournalReadBenchmarks), output, timeoutDurationSeconds: 120)
    {
        Fixture = new SqlServerFixture(true);
    }

    protected override Fixture Fixture { get; }
}

public class PostgreSqlJournalReadBenchmarks : JournalReadBenchmarks
{
    public PostgreSqlJournalReadBenchmarks(ITestOutputHelper output)
        : base(nameof(PostgreSqlJournalReadBenchmarks), output)
    {
        Fixture = new PostgreSqlFixture(true);
    }
    
    protected override Fixture Fixture { get; }
}

public class MySqlJournalReadBenchmarks : JournalReadBenchmarks
{
    public MySqlJournalReadBenchmarks(ITestOutputHelper output)
        : base(nameof(MySqlJournalReadBenchmarks), output)
    {
        Fixture = new MySqlFixture(true);
    }
    
    protected override Fixture Fixture { get; }
}

public class MongoDbJournalReadBenchmarks : JournalReadBenchmarks
{
    public MongoDbJournalReadBenchmarks(ITestOutputHelper output)
        : base(nameof(MongoDbJournalReadBenchmarks), output)
    {
        Fixture = new MongoDbFixture(false);
    }
    
    protected override Fixture Fixture { get; }
}

public class RedisJournalReadBenchmarks : JournalReadBenchmarks
{
    public RedisJournalReadBenchmarks(ITestOutputHelper output)
        : base(nameof(RedisJournalReadBenchmarks), output)
    {
        Fixture = new RedisFixture(false);
    }
    
    protected override Fixture Fixture { get; }
}

public class AzureTableJournalReadBenchmarks : JournalReadBenchmarks
{
    public AzureTableJournalReadBenchmarks(ITestOutputHelper output)
        : base(nameof(AzureTableJournalReadBenchmarks), output)
    {
        Fixture = new AzuriteFixture(false);
    }
    
    protected override Fixture Fixture { get; }
}
