using LiteNetLib.Utils;
using Simulation.Application.DTOs;

namespace Simulation.Networking.DTOs.Intents;

public struct AttackIntentPacket : INetSerializable
{
    public int AttackerCharId;
    public void FromDTO(in AttackIntent dto) => AttackerCharId = dto.AttackerCharId;
    public AttackIntent ToDTO() => new(AttackerCharId);
    public void Serialize(NetDataWriter writer) => writer.Put(AttackerCharId);
    public void Deserialize(NetDataReader reader) => AttackerCharId = reader.GetInt();
}
