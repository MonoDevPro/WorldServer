using Simulation.Core.Abstractions.Intents.In;

namespace Simulation.Core.Abstractions.In;

public interface IIntentProducer
{
    void EnqueueMoveIntent(in MoveIntent intent);
    void EnqueueTeleportIntent(in TeleportIntent intent);
    void EnqueueAttackIntent(in AttackIntent intent);
    void EnqueueEnterGameIntent(in EnterGameIntent intent); // Adicionado
    void EnqueueExitGameIntent(in ExitGameIntent intent);   // Adicionado
}