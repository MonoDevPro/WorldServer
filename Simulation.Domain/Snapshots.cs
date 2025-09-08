using Simulation.Domain.Templates;

namespace Simulation.Domain;

public struct JoinAckSnapshot { public int MapId, YourCharId; public List<PlayerData> Others; }
public struct PlayerJoinedSnapshot { public PlayerData NewPlayer; }
public struct PlayerLeftSnapshot{ public PlayerData LeftPlayer; }
public struct AttackSnapshot { public int CharId; }
public struct MoveSnapshot { public int CharId; public Position Old, New; }
public struct TeleportSnapshot { public int CharId, MapId; public Position Position; }
