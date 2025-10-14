using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Persistence.Benchmarks;

public class AggregatorActor: UntypedActor
{
    private readonly TaskCompletionSource _completionTcs;
    private readonly TaskCompletionSource _startupTcs;
    private readonly int _totalActors;
    private int _currentCount;
    
    public AggregatorActor(TaskCompletionSource completionTcs, TaskCompletionSource startupTcs, int totalActors)
    {
        _completionTcs = completionTcs;
        _totalActors = totalActors;
        _startupTcs = startupTcs;
    }

    protected override void PreStart()
    {
        _startupTcs.SetResult();
    }

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case Done:
                Context.Watch(Sender);
                _currentCount++;
                if(_currentCount >= _totalActors)
                    _completionTcs.SetResult();
                return;
        }
        
        Unhandled(message);
    }
}