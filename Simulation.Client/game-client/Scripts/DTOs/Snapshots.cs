using GameClient.Scripts.Domain;

namespace GameClient.Scripts.DTOs;

public record struct AttackSnapshot(int CharId);
public record struct MoveSnapshot(int CharId, Position Old, Position New);
public record struct TeleportSnapshot(int CharId, int MapId, Position Position);

public record JoinAckDto(int YourCharId, int YourEntityId, int MapId, IReadOnlyList<PlayerStateDto> Others);
public record PlayerJoinedDto(PlayerStateDto NewPlayer);
public record PlayerLeftDto(PlayerStateDto LeftPlayer);
