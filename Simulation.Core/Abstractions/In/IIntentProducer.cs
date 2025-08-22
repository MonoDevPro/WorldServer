namespace Simulation.Core.Abstractions.In;

public interface IIntentProducer
{
    void EnqueueMoveIntent(in MoveIntent intent);
    void EnqueueTeleportIntent(in TeleportIntent intent);
    void EnqueueAttackIntent(in AttackIntent intent);
}