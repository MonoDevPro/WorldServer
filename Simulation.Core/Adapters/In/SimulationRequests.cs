using Simulation.Core.Abstractions.In;

namespace Simulation.Core.Adapters.In;

public class SimulationRequests : ISimulationRequests
{
    private readonly Queue<Requests.Move> _moves = new();
    private readonly Queue<Requests.Teleport> _teleports = new();
    private readonly Queue<Requests.Attack> _attacks = new();
    private readonly object _lock = new();

    public void EnqueueMove(Requests.Move request)
    {
        lock (_lock) _moves.Enqueue(request);
    }

    public void EnqueueTeleport(Requests.Teleport request)
    {
        lock (_lock) _teleports.Enqueue(request);
    }

    public void EnqueueAttack(Requests.Attack request)
    {
        lock (_lock) _attacks.Enqueue(request);
    }

    public bool TryDequeueMove(out Requests.Move request)
    {
        lock (_lock)
        {
            if (_moves.Count > 0)
            {
                request = _moves.Dequeue();
                return true;
            }
        }
        request = default;
        return false;
    }

    public bool TryDequeueTeleport(out Requests.Teleport request)
    {
        lock (_lock)
        {
            if (_teleports.Count > 0)
            {
                request = _teleports.Dequeue();
                return true;
            }
        }
        request = default;
        return false;
    }

    public bool TryDequeueAttack(out Requests.Attack request)
    {
        lock (_lock)
        {
            if (_attacks.Count > 0)
            {
                request = _attacks.Dequeue();
                return true;
            }
        }
        request = default;
        return false;
    }
}