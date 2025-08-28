using Simulation.Core.Abstractions.Adapters;

namespace Simulation.Core.Abstractions.Ports;

public interface IIntentHandler
{
    void EnqueueEnterGameIntent(in EnterGameIntent intent);
    void EnqueueExitGameIntent(in ExitGameIntent intent);
    void EnqueueMoveIntent(in MoveIntent intent);
    void EnqueueTeleportIntent(in TeleportIntent intent);
    void EnqueueAttackIntent(in AttackIntent intent);
}