using System.Linq;
using Akka.Actor;
using Akka.Event;

namespace Akka.Persistence.Benchmarks;

public class Start
{
    public static Start Instance { get; } = new ();
    private Start() { }
}

public record Cmd(PersistenceMode Mode, IActorRef? ReplyTo);

public class BenchActor : UntypedPersistentActor
{
    private readonly ILoggingAdapter _log;
    private readonly int _batchSize;
    private IActorRef? _replyTo;
    
    public BenchActor(string persistenceId, int replyAfter, IActorRef? replyTo, int batchSize = 50)
    {
        PersistenceId = persistenceId;
        ReplyAfter = replyAfter;
        _replyTo = replyTo;
        _batchSize = batchSize;
        
        _log = Context.GetLogger();
    }
    
    public override string PersistenceId { get; }
    
    private int ReplyAfter { get; }
    
    private PersistenceMode _mode;
    
    protected override void OnRecover(object message)
    {
        if(message is RecoveryCompleted)
            _replyTo?.Tell(Done.Instance);
    }

    protected override void OnCommand(object message)
    {
        switch (message)
        {
            case int i when _mode == PersistenceMode.Persist:
            {
                Persist(i, ContinuityHandler);
                return;
            }
            
            case int i when _mode == PersistenceMode.PersistAsync:
            {
                PersistAsync(i, ContinuityHandler);
                return;
            }
            
            case int i when _mode == PersistenceMode.BatchPersist:
            {
                PersistAll(Enumerable.Range(++i, _batchSize), BatchContinuityHandler);
                return;
            }
            
            case int i when _mode == PersistenceMode.BatchPersistAsync:
            {
                PersistAllAsync(Enumerable.Range(++i, _batchSize), BatchContinuityHandler);
                return;
            }

            case Start:
            {
                Sender.Tell(Done.Instance);
                return;
            }
            
            case Cmd cmd:
            {
                _mode = cmd.Mode;
                _replyTo = cmd.ReplyTo ?? Sender;
                Self.Tell(0);
                return;
            }
        }
    }

    private void ContinuityHandler(int i)
    {
        i++;
        if (i < ReplyAfter)
        {
            Self.Tell(i);
        }
        else
        {
            _replyTo!.Tell(Done.Instance);
        }
    }
    
    private void BatchContinuityHandler(int i)
    {
        i++;
        if (i < ReplyAfter)
        {
            if(i % _batchSize == 0)
                Self.Tell(i);
        }
        else
        {
            _replyTo!.Tell(Done.Instance);
        }
    }
}