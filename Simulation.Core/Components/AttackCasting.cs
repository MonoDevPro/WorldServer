using Arch.Core;
using Simulation.Core.Commons;

namespace Simulation.Core.Components;

public enum AttackType
{
    /// <summary>
    /// Ataque corpo a corpo.
    /// </summary>
    Melee,
    /// <summary>
    /// Ataque à distância.
    /// </summary>
    Ranged,
    /// <summary>
    /// Ataque em área ou mágico, que pode afetar múltiplos alvos.
    /// </summary>
    AreaOfEffect
}

public struct AttackCasting
{
    public AttackType Type;
    
    // Alvo para ataques Melee/Ranged
    public Entity TargetEntity; 
    
    // Posição central para ataques AreaOfEffect
    public GameVector2 TargetPosition;
    
    // Raio para ataques AreaOfEffect
    public float Radius;
}