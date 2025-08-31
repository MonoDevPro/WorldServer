using LiteNetLib.Utils;
using Simulation.Application.DTOs;

namespace Simulation.Networking.DTOs.Intents;

public struct ExitIntentPacket : INetSerializable
{
    public int CharId;
    public void FromDTO(in ExitIntent dto) => CharId = dto.CharId;
    public ExitIntent ToDTO() => new(CharId);
    public void Serialize(NetDataWriter writer) => writer.Put(CharId);
    public void Deserialize(NetDataReader reader) => CharId = reader.GetInt();
}
