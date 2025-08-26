using LiteNetLib.Utils;

namespace Simulation.Core.Abstractions.Intents.Out;

public struct CharExitSnapshot : INetSerializable
{
    public int CharId;
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CharId);
    }

    public void Deserialize(NetDataReader reader)
    {
        CharId = reader.GetInt();
    }
}