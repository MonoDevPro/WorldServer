using LiteNetLib.Utils;
using Simulation.Application.DTOs;

namespace Simulation.Networking.DTOs.Snapshots;

public struct ExitSnapshotPacket : INetSerializable
{
    public int CharId;
    public void FromDTO(in ExitSnapshot dto) => CharId = dto.CharId;
    public ExitSnapshot ToDTO() => new(CharId);
    public void Serialize(NetDataWriter writer) => writer.Put(CharId);
    public void Deserialize(NetDataReader reader) => CharId = reader.GetInt();
}