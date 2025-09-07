using Simulation.Domain.Components;

namespace Simulation.Application.DTOs.Snapshots;

// Character Snapshots
public struct JoinAckSnapshot { public int YourCharId, MapId; public List<PlayerState> Others; }
public struct PlayerJoinedSnapshot { public PlayerState NewPlayer; }
public struct PlayerLeftSnapshot{ public PlayerState LeftPlayer; }
public struct AttackSnapshot { public int CharId; }
public struct MoveSnapshot { public int CharId; public Position Old, New; }
public struct TeleportSnapshot { public int CharId, MapId; public Position Position; }
