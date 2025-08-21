using Simulation.Core.Abstractions.Out;

namespace Simulation.Core.Adapters.Out;

public class SnapshotEvents : ISnapshotEvents
{
    public event Action<Snapshots.MoveSnapshot> OnMoveSnapshot = delegate { };
    public event Action<Snapshots.AttackSnapshot> OnAttackSnapshot = delegate { };

    public void RaiseMoveSnapshot(in Snapshots.MoveSnapshot snapshot)
        => OnMoveSnapshot.Invoke(snapshot);

    public void RaiseAttackSnapshot(in Snapshots.AttackSnapshot snapshot)
        => OnAttackSnapshot.Invoke(snapshot);

    public void Dispose() { }
}