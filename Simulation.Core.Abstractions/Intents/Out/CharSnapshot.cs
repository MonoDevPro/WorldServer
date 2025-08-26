using LiteNetLib.Utils;
using Simulation.Core.Abstractions.Commons.Components;
using Simulation.Core.Abstractions.Commons.Components.Attack;
using Simulation.Core.Abstractions.Commons.Components.Char;
using Simulation.Core.Abstractions.Commons.Components.Move;
using Simulation.Core.Abstractions.Commons.Enums;
using Simulation.Core.Abstractions.Commons.VOs;

namespace Simulation.Core.Abstractions.Intents.Out;

public record struct CharSnapshot(MapRef MapRef, CharId CharId, CharInfo Info, TilePosition Position, Direction Direction, MoveSpeed MoveSpeed, AttackSpeed AttackSpeed) : INetSerializable
{
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(MapRef.MapId);
        writer.Put(CharId.CharacterId);
        writer.Put(Info.Name);
        writer.Put((byte)Info.Gender);
        writer.Put((byte)Info.Vocation);
        writer.Put(Position.Position.X);
        writer.Put(Position.Position.Y);
        writer.Put(Direction.Value.X);
        writer.Put(Direction.Value.Y);
        writer.Put(MoveSpeed.Value);
        writer.Put(AttackSpeed.CastTime);
        writer.Put(AttackSpeed.Cooldown);
    }

    public void Deserialize(NetDataReader reader)
    {
        MapRef = new MapRef
        {
            MapId = reader.GetInt()
        };
        CharId = new CharId
        {
            CharacterId = reader.GetInt()
        };
        Info = new CharInfo
        {
            Name = reader.GetString(),
            Gender = (Gender)reader.GetByte(),
            Vocation = (Vocation)reader.GetByte()
        };
        Position = new TilePosition
        {
            Position = new GameVector2
            {
                X = reader.GetInt(),
                Y = reader.GetInt()
            }
        };
        Direction = new Direction
        {
            Value = new GameVector2
            {
                X = reader.GetInt(),
                Y = reader.GetInt()
            }
        };
        MoveSpeed = new MoveSpeed
        {
            Value = reader.GetFloat()
        };
        AttackSpeed = new AttackSpeed
        {
            CastTime = reader.GetFloat(),
            Cooldown = reader.GetFloat()
        };
    }
}
    
    
