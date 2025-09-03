using LiteNetLib.Utils;
using Simulation.Domain.Components;
using Simulation.Domain.Templates;

namespace Simulation.Networking.DTOs;

public struct CharTemplatePacket : INetSerializable
{
    public string Name;
    public Gender Gender;
    public Vocation Vocation;
    public int CharId;
    public int MapId;
    public Position Position;
    public Direction Direction;
    public float MoveSpeed;
    public float AttackCastTime;
    public float AttackCooldown;

    public CharTemplatePacket()
    {
        Name = string.Empty;
        Gender = default;
        Vocation = default;
        CharId = default;
        MapId = default;
        Position = default;
        Direction = default;
        MoveSpeed = default;
        AttackCastTime = default;
        AttackCooldown = default;
    }

    public void FromDTO(in CharTemplate dto)
    {
        Name = dto.Name;
        Gender = dto.Gender;
        Vocation = dto.Vocation;
        CharId = dto.CharId;
        MapId = dto.MapId;
        Position = dto.Position;
        Direction = dto.Direction;
        MoveSpeed = dto.MoveSpeed;
        AttackCastTime = dto.AttackCastTime;
        AttackCooldown = dto.AttackCooldown;
    }

    public CharTemplate ToDTO() => new()
    {
        Name = Name,
        Gender = Gender, 
        Vocation = Vocation, 
        CharId = CharId, 
        MapId = MapId, 
        Position = Position, 
        Direction = Direction, 
        MoveSpeed = MoveSpeed, 
        AttackCastTime = AttackCastTime, 
        AttackCooldown = AttackCooldown
    };

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Name);
        writer.Put((int)Gender);
        writer.Put((int)Vocation);
        writer.Put(CharId);
        writer.Put(MapId);
        writer.Put(Position.X);
        writer.Put(Position.Y);
        writer.Put(Direction.X);
        writer.Put(Direction.Y);
        writer.Put(MoveSpeed);
        writer.Put(AttackCastTime);
        writer.Put(AttackCooldown);
    }

    public void Deserialize(NetDataReader reader)
    {
        Name = reader.GetString();
        Gender = (Gender)reader.GetInt();
        Vocation = (Vocation)reader.GetInt();
        CharId = reader.GetInt();
        MapId = reader.GetInt();
        Position = new Position { X = reader.GetInt(), Y = reader.GetInt() };
        Direction = new Direction { X = reader.GetInt(), Y = reader.GetInt() };
        MoveSpeed = reader.GetFloat();
        AttackCastTime = reader.GetFloat();
        AttackCooldown = reader.GetFloat();
    }
}