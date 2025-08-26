using Simulation.Core.Abstractions.Intents.In;

namespace Simulation.Core.Abstractions.In;

public interface IIntentProducer
{
    void EnqueueEnterGameIntent(in EnterGameIntent intent);
    void EnqueueExitGameIntent(in ExitGameIntent intent);
    void EnqueueMoveIntent(in MoveIntent intent);
    void EnqueueTeleportIntent(in TeleportIntent intent);
    void EnqueueAttackIntent(in AttackIntent intent);
}