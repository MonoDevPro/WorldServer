using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.In;

public readonly record struct MoveIntent(int CharId, GameVector2 Input);