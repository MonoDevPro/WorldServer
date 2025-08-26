using LiteNetLib.Utils;
using Simulation.Core.Abstractions.Commons.VOs;

namespace Simulation.Core.Abstractions.Intents.In;

public record struct MoveIntent(int CharId, GameVector2 Input): INetSerializable
{
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CharId);
        writer.Put(Input.X);
        writer.Put(Input.Y);
    }

    public void Deserialize(NetDataReader reader)
    {
        CharId = reader.GetInt();
        Input = new GameVector2(reader.GetInt(), reader.GetInt());
    }
}