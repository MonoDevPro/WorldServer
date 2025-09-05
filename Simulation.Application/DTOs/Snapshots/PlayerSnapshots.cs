using Simulation.Domain.Components;

namespace Simulation.Application.DTOs.Snapshots;

// Character Snapshots
public record struct AttackSnapshot(int CharId);
public record struct MoveSnapshot(int CharId, Position Old, Position New);
public record struct TeleportSnapshot(int CharId, int MapId, Position Position);
