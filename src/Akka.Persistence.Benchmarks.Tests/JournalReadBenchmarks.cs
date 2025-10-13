using System.Diagnostics;
using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using Akka.Persistence.Benchmarks.Fixtures;
using Akka.Routing;
using Akka.TestKit;
using Akka.Util.Internal;
using MathNet.Numerics.Statistics;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.Benchmarks;

public abstract class JournalReadBenchmarks: Akka.Hosting.TestKit.TestKit
{
    // Number of measurement iterations each test will be run.
    private const int MeasurementIterations = 101;
    private const double OutlierRejectionSigma = 2;

    // Number of messages sent to the PersistentActor under test for each test iteration
    private readonly int _eventsCount;

    private readonly TimeSpan _expectDuration;
    private TestProbe? _testProbe;
    private readonly IReadOnlyList<int> _commands;

    protected JournalReadBenchmarks(
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
    public async Task PersistenceActor_performance_must_measure_Recovering()
    {
        if (!await Fixture.IsVolumeInitializedAsync("PersistRecoverPid"))
        {
            Log.Info("Database has not been initialized, we'll initialize it now. You shouldn't need to initialize it again as long as you didn't remove the database volume file.");
            
            var p1 = BenchActor("PersistRecoverPid", _eventsCount);

            await FeedAndExpectLastAsync(p1, "p", _commands);
        }

        await MeasureAsync(
            d => $"Recovering {_eventsCount} took {d.TotalMilliseconds} ms",
            async () =>
            {
                BenchActor("PersistRecoverPid", _eventsCount);
                await _testProbe!.ExpectMsgAsync(_commands[^1], _expectDuration);
            });
    }

    [Fact]
    public async Task PersistenceActor_performance_must_measure_RecoveringTwo()
    {
        if (!await Fixture.IsVolumeInitializedAsync("DoublePersistRecoverPid1"))
        {
            Log.Info("Database has not been initialized, we'll initialize it now. You shouldn't need to initialize it again as long as you didn't remove the database volume file.");
            
            var p1 = BenchActorNewProbe("DoublePersistRecoverPid1", _eventsCount);
            var p2 = BenchActorNewProbe("DoublePersistRecoverPid2", _eventsCount);

            await FeedAndExpectLastSpecificAsync(p1, "p", _commands);
            await FeedAndExpectLastSpecificAsync(p2, "p", _commands);
        }
        
        await MeasureGroupAsync(
            d => $"Recovering {_eventsCount} took {d.TotalMilliseconds} ms",
            async () =>
            {
                async Task Task1()
                {
                    var (_, probe) = BenchActorNewProbe("DoublePersistRecoverPid1", _eventsCount);
                    await probe.ExpectMsgAsync(_commands[^1], _expectDuration);
                }

                async Task Task2()
                {
                    var (_, probe) = BenchActorNewProbe("DoublePersistRecoverPid2", _eventsCount);
                    await probe.ExpectMsgAsync(_commands[^1], _expectDuration);
                }

                await Task.WhenAll(Task1(), Task2());
            },
            _eventsCount,
            2);
    }

    [Fact]
    public async Task PersistenceActor_performance_must_measure_RecoveringFour()
    {
        if (!await Fixture.IsVolumeInitializedAsync("QuadPersistRecoverPid1"))
        {
            Log.Info("Database has not been initialized, we'll initialize it now. You shouldn't need to initialize it again as long as you didn't remove the database volume file.");
            
            var p1 = BenchActorNewProbe("QuadPersistRecoverPid1", _eventsCount);
            var p2 = BenchActorNewProbe("QuadPersistRecoverPid2", _eventsCount);
            var p3 = BenchActorNewProbe("QuadPersistRecoverPid3", _eventsCount);
            var p4 = BenchActorNewProbe("QuadPersistRecoverPid4", _eventsCount);

            await FeedAndExpectLastSpecificAsync(p1, "p", _commands);
            await FeedAndExpectLastSpecificAsync(p2, "p", _commands);
            await FeedAndExpectLastSpecificAsync(p3, "p", _commands);
            await FeedAndExpectLastSpecificAsync(p4, "p", _commands);
        }

        await MeasureGroupAsync(
            d => $"Recovering {_eventsCount} took {d.TotalMilliseconds} ms",
            async () =>
            {
                async Task Task1()
                {
                    var (_, probe) = BenchActorNewProbe("QuadPersistRecoverPid1", _eventsCount);
                    await probe.ExpectMsgAsync(_commands[^1], _expectDuration);
                }

                async Task Task2()
                {
                    var (_, probe) = BenchActorNewProbe("QuadPersistRecoverPid2", _eventsCount);
                    await probe.ExpectMsgAsync(_commands[^1], _expectDuration);
                }

                async Task Task3()
                {
                    var (_, probe) = BenchActorNewProbe("QuadPersistRecoverPid3", _eventsCount);
                    await probe.ExpectMsgAsync(_commands[^1], _expectDuration);
                }

                async Task Task4()
                {
                    var (_, probe) = BenchActorNewProbe("QuadPersistRecoverPid4", _eventsCount);
                    await probe.ExpectMsgAsync(_commands[^1], _expectDuration);
                }

                await Task.WhenAll(Task1(), Task2(), Task3(), Task4());
            },
            _eventsCount,
            4);
    }

    [Fact]
    public async Task PersistenceActor_performance_must_measure_Recovering8()
    {
        if (!await Fixture.IsVolumeInitializedAsync("OctPersistRecoverPid1"))
        {
            Log.Info("Database has not been initialized, we'll initialize it now. You shouldn't need to initialize it again as long as you didn't remove the database volume file.");
            
            var p1 = BenchActorNewProbe("OctPersistRecoverPid1", _eventsCount);
            var p2 = BenchActorNewProbe("OctPersistRecoverPid2", _eventsCount);
            var p3 = BenchActorNewProbe("OctPersistRecoverPid3", _eventsCount);
            var p4 = BenchActorNewProbe("OctPersistRecoverPid4", _eventsCount);
            var p5 = BenchActorNewProbe("OctPersistRecoverPid5", _eventsCount);
            var p6 = BenchActorNewProbe("OctPersistRecoverPid6", _eventsCount);
            var p7 = BenchActorNewProbe("OctPersistRecoverPid7", _eventsCount);
            var p8 = BenchActorNewProbe("OctPersistRecoverPid8", _eventsCount);

            await FeedAndExpectLastSpecificAsync(p1, "p", _commands);
            await FeedAndExpectLastSpecificAsync(p2, "p", _commands);
            await FeedAndExpectLastSpecificAsync(p3, "p", _commands);
            await FeedAndExpectLastSpecificAsync(p4, "p", _commands);
            await FeedAndExpectLastSpecificAsync(p5, "p", _commands);
            await FeedAndExpectLastSpecificAsync(p6, "p", _commands);
            await FeedAndExpectLastSpecificAsync(p7, "p", _commands);
            await FeedAndExpectLastSpecificAsync(p8, "p", _commands);
        }

        await MeasureGroupAsync(
            d => $"Recovering {_eventsCount} took {d.TotalMilliseconds} ms , {_eventsCount * 8 / d.TotalMilliseconds * 1000} total msg/sec",
            async () =>
            {
                async Task Task1()
                {
                    var (_, probe) = BenchActorNewProbe("OctPersistRecoverPid1", _eventsCount);
                    await probe.ExpectMsgAsync(_commands[^1], _expectDuration);
                }

                async Task Task2()
                {
                    var (_, probe) = BenchActorNewProbe("OctPersistRecoverPid2", _eventsCount);
                    await probe.ExpectMsgAsync(_commands[^1], _expectDuration);
                }

                async Task Task3()
                {
                    var (_, probe) = BenchActorNewProbe("OctPersistRecoverPid3", _eventsCount);
                    await probe.ExpectMsgAsync(_commands[^1], _expectDuration);
                }

                async Task Task4()
                {
                    var (_, probe) = BenchActorNewProbe("OctPersistRecoverPid4", _eventsCount);
                    await probe.ExpectMsgAsync(_commands[^1], _expectDuration);
                }

                async Task Task5()
                {
                    var (_, probe) = BenchActorNewProbe("OctPersistRecoverPid5", _eventsCount);
                    await probe.ExpectMsgAsync(_commands[^1], _expectDuration);
                }

                async Task Task6()
                {
                    var (_, probe) = BenchActorNewProbe("OctPersistRecoverPid6", _eventsCount);
                    await probe.ExpectMsgAsync(_commands[^1], _expectDuration);
                }

                async Task Task7()
                {
                    var (_, probe) = BenchActorNewProbe("OctPersistRecoverPid7", _eventsCount);
                    await probe.ExpectMsgAsync(_commands[^1], _expectDuration);
                }

                async Task Task8()
                {
                    var (_, probe) = BenchActorNewProbe("OctPersistRecoverPid8", _eventsCount);
                    await probe.ExpectMsgAsync(_commands[^1], _expectDuration);
                }

                await Task.WhenAll(Task1(), Task2(), Task3(), Task4(), Task5(), Task6(), Task7(), Task8());
            },
            _eventsCount,
            8);
    }

}