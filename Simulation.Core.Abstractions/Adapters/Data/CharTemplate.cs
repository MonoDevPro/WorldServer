using LiteNetLib.Utils;
using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.Adapters.Data;

public enum Gender : int { None, Male, Female }
public enum Vocation : int { None, Mage, Archer }
public record CharTemplate: INetSerializable
{
    public string Name { get; set; } = string.Empty;
    public Gender Gender { get; set; } = Gender.None;
    public Vocation Vocation { get; set; } = Vocation.None;
    
    // ECS Identifiers
    public CharId CharId { get; set; }
    public MapId MapId { get; set; }
    public Position Position { get; set; }
    public Direction Direction { get; set; }
    public MoveStats MoveStats { get; set; }
    public AttackStats AttackStats { get; set; }
    public Blocking Blocking { get; set; }
    
    public void Serialize(NetDataWriter writer)
    {
        // Strings + enums
        writer.Put(Name);
        writer.Put((int)Gender);
        writer.Put((int)Vocation);

        // Identificadores
        writer.Put(CharId.Value);
        writer.Put(MapId.Value);

        // Coord/Direction
        writer.Put(Position.Value.X);
        writer.Put(Position.Value.Y);
        writer.Put(Direction.Value.X);
        writer.Put(Direction.Value.Y);

        // Stats
        writer.Put(MoveStats.Speed);
        writer.Put(AttackStats.CastTime);
        writer.Put(AttackStats.Cooldown);
    }

    public void Deserialize(NetDataReader reader)
    {
        // Strings + enums
        Name = reader.GetString();
        Gender = (Gender)reader.GetInt();
        Vocation = (Vocation)reader.GetInt();

        // Identificadores
        CharId = new CharId(reader.GetInt());
        MapId = new MapId(reader.GetInt());

        // Coord/Direction
        var posX = reader.GetInt();
        var posY = reader.GetInt();
        Position = new Position { Value = new GameCoord(posX, posY) };

        var dirX = reader.GetInt();
        var dirY = reader.GetInt();
        Direction = new Direction { Value = new GameDirection(dirX, dirY) };

        // Stats
        MoveStats = new MoveStats { Speed = reader.GetFloat() };
        AttackStats = new AttackStats
        {
            CastTime = reader.GetFloat(),
            Cooldown = reader.GetFloat()
        };
    }
}