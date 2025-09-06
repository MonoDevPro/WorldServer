using GameClient.Scripts.Domain;

namespace GameClient.Scripts.DTOs;

public record PlayerStateDto(
    int CharId,
    int EntityId,
    int MapId,
    Position Position,
    Direction Direction,
    float MoveSpeed,
    float AttackCastTime,
    float AttackCooldown);
