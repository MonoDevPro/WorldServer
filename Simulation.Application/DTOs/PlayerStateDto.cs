using Simulation.Domain.Components;
using Simulation.Domain.Templates;

namespace Simulation.Application.DTOs;

public record PlayerStateDto(
    int CharId,
    int EntityId,
    int MapId,
    Position Position,
    Direction Direction,
    float MoveSpeed,
    float AttackCastTime,
    float AttackCooldown)
{
    public static PlayerStateDto CreateFromTemplate(PlayerTemplate template)
    {
        return new PlayerStateDto(
            CharId: template.CharId,
            EntityId: -1, // To be assigned when the player entity is created
            MapId: template.MapId,
            Position: template.Position,
            Direction: template.Direction, // Default direction
            MoveSpeed: template.MoveSpeed,
            AttackCastTime: template.AttackCastTime,
            AttackCooldown: template.AttackCooldown
        );
    }
}