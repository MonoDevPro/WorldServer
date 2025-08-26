using LiteNetLib.Utils;

namespace Simulation.Core.Abstractions.Intents.In;

public record struct EnterGameIntent(int CharacterId) : INetSerializable
{
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CharacterId);
    }

    public void Deserialize(NetDataReader reader)
    {
        CharacterId = reader.GetInt();
    }
}