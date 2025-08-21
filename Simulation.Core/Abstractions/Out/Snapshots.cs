using Arch.Core;
using Simulation.Core.Commons;

namespace Simulation.Core.Abstractions.Out;

public static class Snapshots
{
    public readonly record struct AttackSnapshot
    {
        readonly Entity Attacker;
        readonly GameVector2 Direction;
    }
    
    public readonly record struct MoveSnapshot
    { 
        readonly Entity Entity; 
        readonly GameVector2 Direction; 
        readonly GameVector2 Position;
    }
}