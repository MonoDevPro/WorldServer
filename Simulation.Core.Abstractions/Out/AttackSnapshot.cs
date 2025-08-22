using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.Out;

public readonly record struct AttackSnapshot(int CharId, GameVector2 Direction);