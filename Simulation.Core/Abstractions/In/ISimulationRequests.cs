using Arch.Core;
using Simulation.Core.Commons;
using Simulation.Core.Components;

namespace Simulation.Core.Abstractions.In;

/// <summary>
/// Interface para enfileirar comandos que serão aplicados pelos sistemas de simulação.
/// Thread-safe para uso pela camada de rede.
/// </summary>
public interface ISimulationRequests
{
    void EnqueueMove(Entity entity, int mapId, VelocityVector direction);
    void EnqueueTeleport(Entity entity, int mapId, TilePosition target);
    void EnqueueMeleeAttack(Entity attacker, Entity target);
    void EnqueueRangedAttack(Entity attacker, Entity target);
    void EnqueueAreaAttack(Entity attacker, GameVector2 targetPosition, float radius);
    /// <summary>Despacha todos os comandos pendentes chamando Apply nos sistemas.</summary>
    void Dispatch();
}