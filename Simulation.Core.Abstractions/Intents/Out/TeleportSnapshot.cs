using Simulation.Core.Abstractions.Commons.VOs;

namespace Simulation.Core.Abstractions.Intents.Out;

public readonly record struct TeleportSnapshot(int CharId, GameVector2 Position);