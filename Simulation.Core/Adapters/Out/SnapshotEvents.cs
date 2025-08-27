using Arch.Bus;
using Simulation.Core.Abstractions.Intents.Out;
using Simulation.Core.Abstractions.Out;

namespace Simulation.Core.Adapters.Out;

public partial class SnapshotEvents : ISnapshotEvents
{
    public event Action<GameSnapshot> OnEnterGameSnapshot = delegate { };
    public event Action<CharExitSnapshot> OnCharExitSnapshot = delegate { };
    public event Action<MoveSnapshot> OnMoveSnapshot = delegate { };
    public event Action<AttackSnapshot> OnAttackSnapshot = delegate { };
    
    [Event(order: 0)]
    public void RaiseEnterGameSnapshot(in GameSnapshot snapshot)
        => OnEnterGameSnapshot.Invoke(snapshot);
    
    [Event(order: 0)]
    public void RaiseExitGameSnapshot(in CharExitSnapshot snapshot)
        => OnCharExitSnapshot.Invoke(snapshot);

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