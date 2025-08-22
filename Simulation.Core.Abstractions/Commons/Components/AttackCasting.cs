using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Commons.Enums;

namespace Simulation.Core.Components;

public struct AttackCasting
{
    public AttackType Type;
    
    // Alvo para ataques Melee/Ranged
    public int TargetCharId;
    
    // Posição central para ataques AreaOfEffect
    public GameVector2 TargetPosition;
    
    // Raio para ataques AreaOfEffect
    public float Radius;
}