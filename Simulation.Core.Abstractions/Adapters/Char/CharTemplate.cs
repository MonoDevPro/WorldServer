using LiteNetLib.Utils;
using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.Adapters.Char;

public enum Gender : int { None, Male, Female }
public enum Vocation : int { None, Mage, Archer }

public class CharTemplate: INetSerializable
{
    public string Name = string.Empty;
    public Gender Gender;
    public Vocation Vocation;
    
    // ECS Identifiers
    public int CharId;
    public int MapId;
    public Position Position;
    public Direction Direction;
    public float MoveSpeed;
    public float AttackCastTime;
    public float AttackCooldown;
    
    public void Serialize(NetDataWriter writer)
    {
        // Strings + enums
        writer.Put(Name);
        writer.Put((int)Gender);
        writer.Put((int)Vocation);

        // Identificadores
        writer.Put(CharId);
        writer.Put(MapId);

        // Coord/Direction
        writer.Put(Position.X);
        writer.Put(Position.Y);
        writer.Put(Direction.X);
        writer.Put(Direction.Y);

        // Stats
        writer.Put(MoveSpeed);
        writer.Put(AttackCastTime);
        writer.Put(AttackCooldown);
    }

    public void Deserialize(NetDataReader reader)
    {
        // Strings + enums
        Name = reader.GetString();
        Gender = (Gender)reader.GetInt();
        Vocation = (Vocation)reader.GetInt();

        // Identificadores
        CharId = reader.GetInt();
        MapId = reader.GetInt();

        // Coord/Direction
        var posX = reader.GetInt();
        var posY = reader.GetInt();
        Position = new Position { X = posX, Y = posY };

        var dirX = reader.GetInt();
        var dirY = reader.GetInt();
        Direction = new Direction { X = dirX, Y = dirY };

        // Stats
        MoveSpeed = reader.GetFloat();
        AttackCastTime = reader.GetFloat();
        AttackCooldown = reader.GetFloat();
    }
}