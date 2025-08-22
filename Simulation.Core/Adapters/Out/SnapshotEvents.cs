using Arch.Bus;
using Simulation.Core.Abstractions.Out;

namespace Simulation.Core.Adapters.Out;

public partial class SnapshotEvents : ISnapshotEvents
{
    public event Action<MoveSnapshot> OnMoveSnapshot = delegate { };
    public event Action<AttackSnapshot> OnAttackSnapshot = delegate { };

    [Event(order: 0)]
    public void RaiseMoveSnapshot(in MoveSnapshot snapshot)
        => OnMoveSnapshot.Invoke(snapshot);

    [Event(order: 0)]
    public void RaiseAttackSnapshot(in AttackSnapshot snapshot)
        => OnAttackSnapshot.Invoke(snapshot);

    
    public SnapshotEvents()
    {
        // Initialize any necessary resources here if needed
        Hook();
    }
    public void Dispose()
    {
        Unhook();
    }
}