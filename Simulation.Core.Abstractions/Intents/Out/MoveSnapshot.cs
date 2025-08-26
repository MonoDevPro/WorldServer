using LiteNetLib.Utils;
using Simulation.Core.Abstractions.Commons.VOs;

namespace Simulation.Core.Abstractions.Intents.Out;

public record struct MoveSnapshot(int CharId, GameVector2 Direction, GameVector2 Position): INetSerializable
{
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CharId);
        writer.Put(Direction.X);
        writer.Put(Direction.Y);
        writer.Put(Position.X);
        writer.Put(Position.Y);
    }

    public void Deserialize(NetDataReader reader)
    {
        CharId = reader.GetInt();
        Direction = new GameVector2(reader.GetInt(), reader.GetInt());
        Position = new GameVector2(reader.GetInt(), reader.GetInt());
    }
}
