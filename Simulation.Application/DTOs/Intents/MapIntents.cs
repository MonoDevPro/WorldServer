namespace Simulation.Application.DTOs.Intents;

// Map Intents
public record struct LoadMapIntent(int MapId);
public record struct UnloadMapIntent(int MapId);