using System.Diagnostics;
using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using Akka.Persistence.Benchmarks.Fixtures;
using Akka.Routing;
using Akka.TestKit;
using Akka.Util;
using Akka.Util.Internal;
using MathNet.Numerics.Statistics;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.Benchmarks;

public abstract class JournalWriteBenchmarks: Akka.Hosting.TestKit.TestKit
{
    // Number of measurement iterations each test will be run.
    private const int MeasurementIterations = 101;
    private const double OutlierRejectionSigma = 2;

    // Number of messages sent to the PersistentActor under test for each test iteration
    private readonly int _eventsCount;

    private readonly TimeSpan _expectDuration;
    private TestProbe? _testProbe;
    private readonly IReadOnlyList<int> _commands;

    protected JournalWriteBenchmarks(
        string actorSystem,
        ITestOutputHelper output,
        int timeoutDurationSeconds = 30,
        int eventsCount = 10000)
        : base(actorSystem, output, startupTimeout: TimeSpan.FromSeconds(60))
    {
        _eventsCount = eventsCount;
        _expectDuration = TimeSpan.FromSeconds(timeoutDurationSeconds);
        _commands = Enumerable.Range(1, _eventsCount).ToArray();
    }
    
    protected abstract Fixture Fixture { get; }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        Fixture.ConfigureAkka(builder, provider);
    }

    protected override Task BeforeTestStart()
    {
        _testProbe = CreateTestProbe();
        return Task.CompletedTask;
    }

    protected override async Task AfterAllAsync()
    {
        await Fixture.Container.StopAsync();
    }

    private IActorRef BenchActor(string pid, int replyAfter)
        => Sys.ActorOf(Props.Create(() => new BenchActor(pid, _testProbe!, replyAfter)));

    private (IActorRef aut, TestProbe probe) BenchActorNewProbe(string pid, int replyAfter)
    {
        var tp = CreateTestProbe();
        return (Sys.ActorOf(Props.Create(() => new BenchActor(pid, tp, replyAfter))), tp);
    }

    private (IActorRef aut, TestProbe probe) BenchActorNewProbeGroup(string pid, int numActors, int numMessages)
    {
        var tp = CreateTestProbe();
        return (Sys.ActorOf(
            Props
                .Create(() => new BenchActor(pid, tp, numMessages, false))
                .WithRouter(new RoundRobinPool(numActors))), tp);
    }

    private async Task FeedAndExpectLastRouterSetAsync(
        (IActorRef actor, TestProbe probe) autSet,
        string mode,
        IReadOnlyList<int> commands,
        int numExpect)
    {
        commands.ForEach(c => autSet.actor.Tell(new Broadcast(new Cmd(mode, c))));

        for (var i = 0; i < numExpect; i++)
            await autSet.probe.ExpectMsgAsync(commands[^1], _expectDuration);
    }

    private async Task FeedAndExpectLastAsync(IActorRef actor, string mode, IReadOnlyList<int> commands)
    {
        commands.ForEach(c => actor.Tell(new Cmd(mode, c)));
        await _testProbe!.ExpectMsgAsync(commands[^1], _expectDuration);
    }

    internal async Task FeedAndExpectLastGroupAsync(
        (IActorRef actor, TestProbe probe)[] autSet,
        string mode,
        IReadOnlyList<int> commands)
    {
        foreach (var (actor, _) in autSet)
            commands.ForEach(c => actor.Tell(new Cmd(mode, c)));

        foreach (var (_, probe) in autSet)
            await probe.ExpectMsgAsync(commands[^1], _expectDuration);
    }

    private async Task FeedAndExpectLastSpecificAsync(
        (IActorRef actor, TestProbe probe) aut,
        string mode,
        IReadOnlyList<int> commands)
    {
        commands.ForEach(c => aut.actor.Tell(new Cmd(mode, c)));

        await aut.probe.ExpectMsgAsync(commands[^1], _expectDuration);
    }

    private async Task MeasureAsync(Func<TimeSpan, string> msg, Func<Task> block)
    {
        var measurements = new List<TimeSpan>(MeasurementIterations);

        await block(); // warm-up

        var i = 0;
        while (i < MeasurementIterations)
        {
            var sw = Stopwatch.StartNew();
            await block();
            sw.Stop();
            measurements.Add(sw.Elapsed);
            Output!.WriteLine(msg(sw.Elapsed));
            i++;
        }

        var (rejected, times) = RejectOutliers(measurements.Select(c => c.TotalMilliseconds).ToArray(), OutlierRejectionSigma);

        var mean = times.Average();
        var stdDev = times.PopulationStandardDeviation();
        var min = times.Minimum();
        var q1 = times.LowerQuartile();
        var median = times.Median();
        var q3 = times.UpperQuartile();
        var max = times.Maximum();
        
        Output!.WriteLine($"Mean: {mean:F2} ms, Standard Deviation: {stdDev:F2} ms, Min: {min:F2} ms, Q1: {q1:F2} ms, Median: {median:F2} ms, Q3: {q3:F2} ms, Max: {max:F2} ms");

        var msgPerSec = _eventsCount / mean * 1000;
        Output.WriteLine($"Mean throughput: {msgPerSec:F2} msg/s");
        
        var medianMsgPerSec = _eventsCount / median * 1000;
        Output.WriteLine($"Median throughput: {medianMsgPerSec:F2} msg/s");
        
        Output.WriteLine($"Rejected outlier (sigma: {OutlierRejectionSigma}): {string.Join(", ", rejected)}");
    }

    internal async Task MeasureGroupAsync(Func<TimeSpan, string> msg, Func<Task> block, int numMsg, int numGroup)
    {
        var measurements = new List<TimeSpan>(MeasurementIterations);

        await block();
        await block(); // warm-up

        var i = 0;
        while (i < MeasurementIterations)
        {
            var sw = Stopwatch.StartNew();
            await block();
            sw.Stop();
            measurements.Add(sw.Elapsed);
            Output!.WriteLine(msg(sw.Elapsed));
            i++;
        }

        var (rejected, times) = RejectOutliers(measurements.Select(c => c.TotalMilliseconds).ToArray(), OutlierRejectionSigma);

        var mean = times.Average();
        var stdDev = times.PopulationStandardDeviation();
        var min = times.Minimum();
        var q1 = times.LowerQuartile();
        var median = times.Median();
        var q3 = times.UpperQuartile();
        var max = times.Maximum();
        
        Output!.WriteLine($"Workers: {numGroup}, Mean: {mean:F2} ms, Standard Deviation: {stdDev:F2} ms, Min: {min:F2} ms, Q1: {q1:F2} ms, Median: {median:F2} ms, Q3: {q3:F2} ms, Max: {max:F2} ms");

        var msgPerSec = numMsg / mean * 1000;
        var msgPerSecTotal = numMsg * numGroup / mean * 1000;
        
        Output.WriteLine($"Mean throughput: {msgPerSec:F2} msg/s/actor, Mean total throughput: {msgPerSecTotal:F2} msg/s");
        
        var medianMsgPerSec = numMsg / median * 1000;
        var medianMsgPerSecTotal = numMsg * numGroup / median * 1000;
        Output.WriteLine($"Median throughput: {medianMsgPerSec:F2} msg/s/actor, Median total throughput: {medianMsgPerSecTotal:F2} msg/s");
        
        Output.WriteLine($"Rejected outlier (sigma: {OutlierRejectionSigma}): {string.Join(", ", rejected)}");
    }

    private static (IReadOnlyList<double> Rejected, IReadOnlyList<double> Measurements) RejectOutliers(IReadOnlyList<double> measurements, double sigma)
    {
        var mean = measurements.Average();
        var stdDev = measurements.PopulationStandardDeviation();
        var threshold = sigma * stdDev;
        var minThreshold = mean - threshold;
        var maxThreshold = mean + threshold;
        var rejected = measurements.Where(m => m < minThreshold || m > maxThreshold);
        var accepted = measurements.Where(m => m >= minThreshold && m <= maxThreshold);
        return (rejected.ToArray(), accepted.ToArray());
    }

    [Fact]
    public async Task PersistenceActor_performance_must_measure_Persist()
    {
        var p1 = BenchActor("PersistPid", _eventsCount);

        await MeasureAsync(
            d =>
                $"Persist()-ing {_eventsCount} took {d.TotalMilliseconds} ms",
            async () =>
            {
                await FeedAndExpectLastAsync(p1, "p", _commands);
                p1.Tell(ResetCounter.Instance);
            });
    }

    [Fact]
    public async Task PersistenceActor_performance_must_measure_PersistDouble()
    {
        var p1 = BenchActorNewProbe("DoublePersistPid1", _eventsCount);
        var p2 = BenchActorNewProbe("DoublePersistPid2", _eventsCount);
        await MeasureAsync(
            d => $"Persist()-ing {_eventsCount} took {d.TotalMilliseconds} ms",
            async () =>
            {
                await FeedAndExpectLastGroupAsync([p1, p2], "p", _commands);
                p1.aut.Tell(ResetCounter.Instance);
                p2.aut.Tell(ResetCounter.Instance);
            });
    }

    [Fact]
    public async Task PersistenceActor_performance_must_measure_PersistTriple()
    {
        var p1 = BenchActorNewProbe("TriplePersistPid1", _eventsCount);
        var p2 = BenchActorNewProbe("TriplePersistPid2", _eventsCount);
        var p3 = BenchActorNewProbe("TriplePersistPid3", _eventsCount);
        await MeasureAsync(
            d =>
                $"Persist()-ing {_eventsCount} took {d.TotalMilliseconds} ms",
            async () =>
            {
                var t1 = FeedAndExpectLastSpecificAsync(p1, "p", _commands);
                var t2 = FeedAndExpectLastSpecificAsync(p2, "p", _commands);
                var t3 = FeedAndExpectLastSpecificAsync(p3, "p", _commands);
                await Task.WhenAll(t1, t2, t3);
                p1.aut.Tell(ResetCounter.Instance);
                p2.aut.Tell(ResetCounter.Instance);
                p3.aut.Tell(ResetCounter.Instance);
            });
    }

    [Fact]
    public async Task PersistenceActor_performance_must_measure_PersistGroup10()
    {
        const int numGroup = 10;
        var numCommands = Math.Min(_eventsCount / 10, 1000);
        await RunGroupBenchmarkAsync(numGroup, numCommands);
    }

    [Fact]
    public async Task PersistenceActor_performance_must_measure_PersistGroup25()
    {
        const int numGroup = 25;
        var numCommands = Math.Min(_eventsCount / 25, 1000);
        await RunGroupBenchmarkAsync(numGroup, numCommands);
    }

    [Fact]
    public async Task PersistenceActor_performance_must_measure_PersistGroup50()
    {
        const int numGroup = 50;
        var numCommands = Math.Min(_eventsCount / 50, 1000);
        await RunGroupBenchmarkAsync(numGroup, numCommands);
    }

    [Fact]
    public async Task PersistenceActor_performance_must_measure_PersistGroup100()
    {
        const int numGroup = 100;
        var numCommands = Math.Min(_eventsCount / 100, 1000);
        await RunGroupBenchmarkAsync(numGroup, numCommands);
    }

    [Fact]
    public async Task PersistenceActor_performance_must_measure_PersistGroup200()
    {
        const int numGroup = 200;
        var numCommands = Math.Min(_eventsCount / 100, 500);
        await RunGroupBenchmarkAsync(numGroup, numCommands);
    }

    [Fact]
    public async Task PersistenceActor_performance_must_measure_PersistGroup400()
    {
        const int numGroup = 400;
        var numCommands = Math.Min(_eventsCount / 100, 500);
        await RunGroupBenchmarkAsync(numGroup, numCommands);
    }

    protected async Task RunGroupBenchmarkAsync(int numGroup, int numCommands)
    {
        var p1 = BenchActorNewProbeGroup("GroupPersistPid" + numGroup, numGroup, numCommands);
        var commands = _commands.Take(numCommands).ToArray();
        await MeasureGroupAsync(
            d => $"Persist()-ing {numCommands} * {numGroup} took {d.TotalMilliseconds} ms",
            async () =>
            {
                await FeedAndExpectLastRouterSetAsync(
                    p1,
                    "p",
                    commands,
                    numGroup);

                p1.aut.Tell(new Broadcast(ResetCounter.Instance));
            },
            numCommands,
            numGroup
        );
    }

    [Fact]
    public async Task PersistenceActor_performance_must_measure_PersistQuad()
    {
        //  dotMemory.Check();

        var p1 = BenchActorNewProbe("QuadPersistPid1", _eventsCount);
        var p2 = BenchActorNewProbe("QuadPersistPid2", _eventsCount);
        var p3 = BenchActorNewProbe("QuadPersistPid3", _eventsCount);
        var p4 = BenchActorNewProbe("QuadPersistPid4", _eventsCount);
        await MeasureAsync(
            d =>
                $"Persist()-ing {_eventsCount} took {d.TotalMilliseconds} ms",
            async () =>
            {
                await FeedAndExpectLastGroupAsync([p1,p2,p3,p4],"p", _commands);
                p1.aut.Tell(ResetCounter.Instance);
                p2.aut.Tell(ResetCounter.Instance);
                p3.aut.Tell(ResetCounter.Instance);
                p4.aut.Tell(ResetCounter.Instance);
            });
    }

    [Fact]
    public async Task PersistenceActor_performance_must_measure_PersistOct()
    {
        var p1 = BenchActorNewProbe("OctPersistPid1", _eventsCount);
        var p2 = BenchActorNewProbe("OctPersistPid2", _eventsCount);
        var p3 = BenchActorNewProbe("OctPersistPid3", _eventsCount);
        var p4 = BenchActorNewProbe("OctPersistPid4", _eventsCount);
        var p5 = BenchActorNewProbe("OctPersistPid5", _eventsCount);
        var p6 = BenchActorNewProbe("OctPersistPid6", _eventsCount);
        var p7 = BenchActorNewProbe("OctPersistPid7", _eventsCount);
        var p8 = BenchActorNewProbe("OctPersistPid8", _eventsCount);
        await MeasureAsync(
            d =>
                $"Persist()-ing {_eventsCount} took {d.TotalMilliseconds} ms",
            async () =>
            {
                await FeedAndExpectLastGroupAsync([p1,p2,p3,p4,p5,p6,p7,p8], "p", _commands);
                p1.aut.Tell(ResetCounter.Instance);
                p2.aut.Tell(ResetCounter.Instance);
                p3.aut.Tell(ResetCounter.Instance);
                p4.aut.Tell(ResetCounter.Instance);
                p5.aut.Tell(ResetCounter.Instance);
                p6.aut.Tell(ResetCounter.Instance);
                p7.aut.Tell(ResetCounter.Instance);
                p8.aut.Tell(ResetCounter.Instance);
            });
    }

    [Fact]
    public async Task PersistenceActor_performance_must_measure_PersistAll()
    {
        var p1 = BenchActor("PersistAllPid", _eventsCount);
        await MeasureAsync(
            d => $"PersistAll()-ing {_eventsCount} took {d.TotalMilliseconds} ms",
            async () =>
            {
                await FeedAndExpectLastAsync(p1, "pb", _commands);
                p1.Tell(ResetCounter.Instance);
            });
    }

    [Fact]
    public async Task PersistenceActor_performance_must_measure_PersistAsync()
    {
        var p1 = BenchActor("PersistAsyncPid", _eventsCount);
        await MeasureAsync(
            d => $"PersistAsync()-ing {_eventsCount} took {d.TotalMilliseconds} ms",
            async () =>
            {
                await FeedAndExpectLastAsync(p1, "pa", _commands);
                p1.Tell(ResetCounter.Instance);
            });
    }

    [Fact]
    public async Task PersistenceActor_performance_must_measure_PersistAllAsync()
    {
        var p1 = BenchActor("PersistAllAsyncPid", _eventsCount);
        await MeasureAsync(
            d => $"PersistAllAsync()-ing {_eventsCount} took {d.TotalMilliseconds} ms",
            async () =>
            {
                await FeedAndExpectLastAsync(p1, "pba", _commands);
                p1.Tell(ResetCounter.Instance);
            });
    }

}

internal class ResetCounter
{
    private ResetCounter() { }
    public static ResetCounter Instance { get; } = new();
}

public class Cmd
{
    public Cmd(string mode, int payload)
    {
        Mode = mode;
        Payload = payload;
    }

    public string Mode { get; }

    public int Payload { get; }
}

internal class BenchActor : UntypedPersistentActor
{
    private const int BatchSize = 50;
    private List<Cmd> _batch = new(BatchSize);
    private int _counter;

    public BenchActor(string persistenceId, IActorRef replyTo, int replyAfter, bool groupName)
    {
        PersistenceId = persistenceId + MurmurHash.StringHash(Context.Parent.Path.Name + Context.Self.Path.Name);
        ReplyTo = replyTo;
        ReplyAfter = replyAfter;
    }

    public BenchActor(string persistenceId, IActorRef replyTo, int replyAfter)
    {
        PersistenceId = persistenceId;
        ReplyTo = replyTo;
        ReplyAfter = replyAfter;
    }

    public override string PersistenceId { get; }

    public IActorRef ReplyTo { get; }

    public int ReplyAfter { get; }

    protected override void OnRecover(object message)
    {
        switch (message)
        {
            case Cmd c:
                _counter++;

                if (c.Payload != _counter)
                    throw new ArgumentException($"Expected to receive [{_counter}] yet got: [{c.Payload}]");

                if (_counter == ReplyAfter)
                    ReplyTo.Tell(c.Payload);

                break;
        }
    }

    protected override void OnCommand(object message)
    {
        switch (message)
        {
            case Cmd { Mode: "p" } c:
                Persist(
                    c,
                    d =>
                    {
                        _counter += 1;
                        if (d.Payload != _counter)
                            throw new ArgumentException($"Expected to receive [{_counter}] yet got: [{d.Payload}]");
                        if (_counter == ReplyAfter)
                            ReplyTo.Tell(d.Payload);
                    });

                break;

            case Cmd { Mode: "pb" } c:
                _batch.Add(c);

                if (_batch.Count % BatchSize == 0)
                {
                    PersistAll(
                        _batch,
                        d =>
                        {
                            _counter += 1;
                            if (d.Payload != _counter)
                                throw new ArgumentException(
                                    $"Expected to receive [{_counter}] yet got: [{d.Payload}]");
                            if (_counter == ReplyAfter)
                                ReplyTo.Tell(d.Payload);
                        });
                    _batch = new List<Cmd>(BatchSize);
                }

                break;

            case Cmd { Mode: "pa" } c:
                PersistAsync(
                    c,
                    d =>
                    {
                        _counter += 1;
                        if (d.Payload != _counter)
                            throw new ArgumentException($"Expected to receive [{_counter}] yet got: [{d.Payload}]");
                        if (_counter == ReplyAfter)
                            ReplyTo.Tell(d.Payload);
                    });

                break;

            case Cmd { Mode: "pba" } c:
                _batch.Add(c);

                if (_batch.Count % BatchSize == 0)
                {
                    PersistAllAsync(
                        _batch,
                        d =>
                        {
                            _counter += 1;
                            if (d.Payload != _counter)
                                throw new ArgumentException(
                                    $"Expected to receive [{_counter}] yet got: [{d.Payload}]");
                            if (_counter == ReplyAfter)
                                ReplyTo.Tell(d.Payload);
                        });
                    _batch = new List<Cmd>(BatchSize);
                }

                break;

            case ResetCounter:
                _counter = 0;
                break;
        }
    }

}
