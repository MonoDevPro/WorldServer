using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.Out;

public readonly record struct MoveSnapshot(int CharId, GameVector2 Direction, GameVector2 Position);