using Simulation.Core.Abstractions.Out.DTOs;

namespace Simulation.Core.Abstractions.Out;

public interface ISnapshotEvents : IDisposable
{
    public event Action<MoveSnapshot> OnMoveSnapshot;
    public event Action<AttackSnapshot> OnAttackSnapshot;
}