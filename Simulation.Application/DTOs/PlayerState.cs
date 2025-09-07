using Simulation.Domain.Components;
using Simulation.Domain.Templates;

namespace Simulation.Application.DTOs;

public class PlayerState
{
    public int CharId { get; set; }
    public int MapId { get; set; }
    public Position Position { get; set; }
    public Direction Direction { get; set; }
    public float MoveSpeed { get; set; }
    public float AttackCastTime { get; set; }
    public float AttackCooldown { get; set; }
    
    public static PlayerState CreateFromTemplate(PlayerTemplate template)
    {
        return new PlayerState
        {
            CharId = template.CharId,
            MapId = template.MapId,
            Position = new Position { X = template.Position.X, Y = template.Position.Y },
            Direction = new Direction { X = template.Direction.X, Y = template.Direction.Y },
            MoveSpeed = template.MoveSpeed,
            AttackCastTime = template.AttackCastTime,
            AttackCooldown = template.AttackCooldown
        };
    }
}