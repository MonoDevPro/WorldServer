using LiteNetLib.Utils;

namespace Simulation.Core.Abstractions.Intents.Out;

public record struct AttackSnapshot(int CharId): INetSerializable
{
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CharId);
    }

    public void Deserialize(NetDataReader reader)
    {
        CharId = reader.GetInt();
    }
}