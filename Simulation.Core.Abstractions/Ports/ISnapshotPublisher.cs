using Simulation.Core.Abstractions.Adapters;

namespace Simulation.Core.Abstractions.Ports;

public interface ISnapshotPublisher : IDisposable
{
    public event Action<EnterSnapshot> OnEnterGameSnapshot;
    public event Action<ExitSnapshot> OnCharExitSnapshot;
    public event Action<MoveSnapshot> OnMoveSnapshot;
    public event Action<AttackSnapshot> OnAttackSnapshot;
}