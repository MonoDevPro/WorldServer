using LiteNetLib.Utils;
using Simulation.Core.Abstractions.Commons.VOs;

namespace Simulation.Core.Abstractions.Intents.Out;

public record struct TeleportSnapshot(int CharId, GameVector2 Position): INetSerializable
{
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CharId);
        writer.Put(Position.X);
        writer.Put(Position.Y);
    }

    public void Deserialize(NetDataReader reader)
    {
        CharId = reader.GetInt();
        Position = new GameVector2(reader.GetInt(), reader.GetInt());
    }
}