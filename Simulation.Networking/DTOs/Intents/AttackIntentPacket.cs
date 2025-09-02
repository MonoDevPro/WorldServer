using LiteNetLib.Utils;
using Simulation.Application.DTOs;

namespace Simulation.Networking.DTOs.Intents;

public struct AttackIntentPacket : INetSerializable
{
    public int CharId;
    public void FromDTO(in AttackIntent dto) => CharId = dto.CharId;
    public AttackIntent ToDTO() => new(CharId);
    public void Serialize(NetDataWriter writer) => writer.Put(CharId);
    public void Deserialize(NetDataReader reader) => CharId = reader.GetInt();
}
