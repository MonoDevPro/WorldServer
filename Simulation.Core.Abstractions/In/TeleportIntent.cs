using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.In;

public readonly record struct TeleportIntent(int CharId, GameVector2 Target);