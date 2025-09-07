using Simulation.Application.DTOs;
using Simulation.Application.Ports.Network.Domain.Models;
using Simulation.Application.Ports.Network.Outbound;
using Simulation.Domain.Components;
using Simulation.Domain.Templates;

namespace Simulation.Networking.Packets;

public class PlayerStateDto : ISerializable
{
    public int CharId { get; set; }
    public int MapId { get; set; }
    public Position Position { get; set; }
    public Direction Direction { get; set; }
    public float MoveSpeed { get; set; }
    public float AttackCastTime { get; set; }
    public float AttackCooldown { get; set; }
    
    public static PlayerStateDto CreateDtoFromState(PlayerState state)
    {
        return new PlayerStateDto
        {
            CharId = state.CharId,
            MapId = state.MapId,
            Position = new Position { X = state.Position.X, Y = state.Position.Y },
            Direction = new Direction { X = state.Direction.X, Y = state.Direction.Y },
            MoveSpeed = state.MoveSpeed,
            AttackCastTime = state.AttackCastTime,
            AttackCooldown = state.AttackCooldown
        };
    }
    
    public static PlayerState CreateStateFromDto(PlayerStateDto dto)
    {
        return new PlayerState
        {
            CharId = dto.CharId,
            MapId = dto.MapId,
            Position = new Position { X = dto.Position.X, Y = dto.Position.Y },
            Direction = new Direction { X = dto.Direction.X, Y = dto.Direction.Y },
            MoveSpeed = dto.MoveSpeed,
            AttackCastTime = dto.AttackCastTime,
            AttackCooldown = dto.AttackCooldown
        };
    }
    
    public void Serialize(INetworkWriter writer)
    {
        writer.WriteInt(CharId);
        writer.WriteInt(MapId);
        writer.WriteInt(Position.X);
        writer.WriteInt(Position.Y);
        writer.WriteInt(Direction.X);
        writer.WriteInt(Direction.Y);
        writer.WriteFloat(MoveSpeed);
        writer.WriteFloat(AttackCastTime);
        writer.WriteFloat(AttackCooldown);
    }

    public void Deserialize(INetworkReader reader)
    {
        CharId = reader.ReadInt();
        MapId = reader.ReadInt();
        Position = new Position
        { X = reader.ReadInt(), Y = reader.ReadInt() };
        Direction = new Direction { X = reader.ReadInt(), Y = reader.ReadInt() };
        MoveSpeed = reader.ReadFloat();
        AttackCastTime = reader.ReadFloat();
        AttackCooldown = reader.ReadFloat();
    }
}