using Simulation.Core.Abstractions.Commons.VOs;

namespace Simulation.Core.Abstractions.Intents.In;

public readonly record struct MoveIntent(int CharId, GameVector2 Input);