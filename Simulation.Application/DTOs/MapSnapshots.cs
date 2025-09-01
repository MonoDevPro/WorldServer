namespace Simulation.Application.DTOs;

// Map Snapshots
public record struct LoadMapSnapshot(int MapId);
public record struct UnloadMapSnapshot(int MapId);