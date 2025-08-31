using LiteNetLib.Utils;
using Simulation.Application.DTOs;

namespace Simulation.Networking.DTOs.Snapshots;

public struct AttackSnapshotPacket : INetSerializable
{
    public int AttackerId;
    public void FromDTO(in AttackSnapshot dto) { AttackerId = dto.CharId; }
    public AttackSnapshot ToDTO() => new(AttackerId);
    public void Serialize(NetDataWriter writer) { writer.Put(AttackerId); }
    public void Deserialize(NetDataReader reader) { AttackerId = reader.GetInt(); }
}