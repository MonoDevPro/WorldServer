using Arch.Bus;
using Arch.Core;
using Arch.System;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Core.Adapters;

public partial class SnapshotPublisherSystem : BaseSystem<World, float>, ISnapshotPublisher
{
    public event Action<EnterSnapshot> OnEnterGameSnapshot = delegate { };
    public event Action<ExitSnapshot> OnCharExitSnapshot = delegate { };
    public event Action<MoveSnapshot> OnMoveSnapshot = delegate { };
    public event Action<AttackSnapshot> OnAttackSnapshot = delegate { };
    
    [Event(order: 0)]
    public void RaiseEnterGameSnapshot(in EnterSnapshot snapshot)
        => OnEnterGameSnapshot.Invoke(snapshot);
    
    [Event(order: 0)]
    public void RaiseExitGameSnapshot(in ExitSnapshot snapshot)
        => OnCharExitSnapshot.Invoke(snapshot);

    [Event(order: 0)]
    public void RaiseMoveSnapshot(in MoveSnapshot snapshot)
        => OnMoveSnapshot.Invoke(snapshot);

    [Event(order: 0)]
    public void RaiseAttackSnapshot(in AttackSnapshot snapshot)
        => OnAttackSnapshot.Invoke(snapshot);

    
    public SnapshotPublisherSystem(World world) : base(world)
    {
        // Initialize any necessary resources here if needed
        Hook();
    }
    public override void Dispose()
    {
        Unhook();
        base.Dispose();
    }
}