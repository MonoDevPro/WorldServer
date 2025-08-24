using Simulation.Core.Abstractions.Commons.VOs;

namespace Simulation.Core.Abstractions.Intents.Out;

public readonly record struct MoveSnapshot(int CharId, GameVector2 Direction, GameVector2 Position);
