using LiteNetLib.Utils;
using Simulation.Application.DTOs;
using Simulation.Domain.Templates;

namespace Simulation.Networking.DTOs.Snapshots;

public struct ExitSnapshotPacket : INetSerializable
{
    public int CharId;
    public int MapId;
    public CharTemplatePacket Template;
    public void FromDTO(in ExitSnapshot dto) { MapId = dto.MapId; CharId = dto.CharId; Template = new CharTemplatePacket(); Template.FromDTO(dto.Template); }
    public ExitSnapshot ToDTO() => new(MapId, CharId, Template.ToDTO());
    public void Serialize(NetDataWriter writer) { writer.Put(MapId); writer.Put(CharId); Template.Serialize(writer); }
    public void Deserialize(NetDataReader reader) { MapId = reader.GetInt(); CharId = reader.GetInt(); Template = new CharTemplatePacket(); Template.Deserialize(reader); }
}