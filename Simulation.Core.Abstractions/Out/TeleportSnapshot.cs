using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.Out;

public readonly record struct TeleportSnapshot(int CharId, GameVector2 Position);