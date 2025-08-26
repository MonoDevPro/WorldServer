using LiteNetLib.Utils;
using Simulation.Core.Abstractions.Commons.VOs;

namespace Simulation.Core.Abstractions.Intents.Out;

public record struct MoveSnapshot(int CharId, GameVector2 OldPosition, GameVector2 NewPosition): INetSerializable
{
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CharId);
        writer.Put(OldPosition.X);
        writer.Put(OldPosition.Y);
        writer.Put(NewPosition.X);
        writer.Put(NewPosition.Y);
    }

    public void Deserialize(NetDataReader reader)
    {
        CharId = reader.GetInt();
        OldPosition = new GameVector2(reader.GetInt(), reader.GetInt());
        NewPosition = new GameVector2(reader.GetInt(), reader.GetInt());
    }
}
