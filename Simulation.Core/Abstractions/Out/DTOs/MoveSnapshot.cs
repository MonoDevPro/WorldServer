using Arch.Core;
using Simulation.Core.Commons;

namespace Simulation.Core.Abstractions.Out.DTOs;

public readonly record struct MoveSnapshot
{ 
    readonly Entity Entity; 
    readonly GameVector2 Direction; 
    readonly GameVector2 Position;
}