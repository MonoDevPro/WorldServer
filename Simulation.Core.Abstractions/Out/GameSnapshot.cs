using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.Out;

public readonly record struct GameSnapshot(
    int MapId,
    IReadOnlyList<CharacterSnapshot> Entities
);