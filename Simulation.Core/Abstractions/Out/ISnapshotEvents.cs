
using Simulation.Core.Abstractions.Intents.Out;

namespace Simulation.Core.Abstractions.Out;

public interface ISnapshotEvents : IDisposable
{
    public event Action<GameSnapshot> OnEnterGameSnapshot;
    public event Action<CharExitSnapshot> OnCharExitSnapshot;
    public event Action<MoveSnapshot> OnMoveSnapshot;
    public event Action<AttackSnapshot> OnAttackSnapshot;
}