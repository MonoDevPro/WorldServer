using LiteNetLib.Utils;
using Simulation.Application.DTOs;

namespace Simulation.Networking.DTOs.Intents;

public struct EnterIntentPacket : INetSerializable
{
    public int CharId;
    public void FromDTO(in EnterIntent dto) => CharId = dto.CharId;
    public EnterIntent ToDTO() => new(CharId);
    public void Serialize(NetDataWriter writer) => writer.Put(CharId);
    public void Deserialize(NetDataReader reader) => CharId = reader.GetInt();
}
