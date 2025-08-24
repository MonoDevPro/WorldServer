namespace Simulation.Core.Abstractions.Intents.Out;

public readonly record struct GameSnapshot(
    int MapId,
    IReadOnlyList<CharacterSnapshot> Entities
);