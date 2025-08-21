using Arch.Core;
using Simulation.Core.Commons;
using Simulation.Core.Commons.Enums;
using Simulation.Core.Components;

namespace Simulation.Core.Abstractions.In;

public static class Requests
{
    public readonly record struct Move(Entity Entity, int MapId, DirectionInput Input);
    
    public readonly record struct Teleport(Entity Entity, int MapId, TilePosition Target);

    /// <summary>
    /// Comando unificado para iniciar qualquer tipo de ataque.
    /// </summary>
    public readonly record struct Attack(
        Entity Attacker,
        AttackType Type,
        Entity TargetEntity = default, // Usado para Melee/Ranged
        GameVector2 TargetPosition = default, // Usado para AreaOfEffect
        float Radius = 0f // Usado para AreaOfEffect
    );
}