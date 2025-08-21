
namespace Simulation.Core.Abstractions.Out;

public interface ISnapshotEvents : IDisposable
{
    public event Action<Snapshots.MoveSnapshot> OnMoveSnapshot;
    public event Action<Snapshots.AttackSnapshot> OnAttackSnapshot;
}