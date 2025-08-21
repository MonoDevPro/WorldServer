using Arch.Bus;
using Simulation.Core.Abstractions.Out;
using Simulation.Core.Abstractions.Out.DTOs;

namespace Simulation.Core.Adapters.Out;

public partial class SnapshotEvents : ISnapshotEvents
{
    public event Action<MoveSnapshot> OnMoveSnapshot = delegate { };
    public event Action<AttackSnapshot> OnAttackSnapshot = delegate { };
    
    [Event(0)]
    public void RaiseMoveSnapshot(ref MoveSnapshot snapshot)
        => OnMoveSnapshot.Invoke(snapshot);
    [Event(0)]
    public void RaiseAttackSnapshot(ref AttackSnapshot snapshot)
        => OnAttackSnapshot.Invoke(snapshot);
    
    public SnapshotEvents()
    {
        Hook();
    }
    
    public void Dispose()
    {
        Unhook();
    }
}