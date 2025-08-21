using Arch.Core;
using Simulation.Core.Commons;

namespace Simulation.Core.Abstractions.Out.DTOs;

public readonly record struct AttackSnapshot
{
    readonly Entity Attacker;
    readonly GameVector2 Direction;
}