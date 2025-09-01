namespace Simulation.Application.DTOs;

// Map Intents
public record struct LoadMapIntent(int MapId);
public record struct UnloadMapIntent(int MapId);