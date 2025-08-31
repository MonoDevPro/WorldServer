using LiteNetLib.Utils;
using Simulation.Application.DTOs;

namespace Simulation.Networking.DTOs.Snapshots;

public struct CharSnapshotPacket : INetSerializable
{
    public int CharId;
    public int MapId;
    public CharTemplatePacket Template;

    public void FromDTO(in CharSnapshot dto)
    {
        CharId = dto.CharId;
        MapId = dto.MapId;
        Template = new CharTemplatePacket();
        Template.FromDTO(dto.Template);
    }

    public CharSnapshot ToDTO() => new(MapId, CharId, Template.ToDTO());
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CharId);
        writer.Put(MapId);
        Template.Serialize(writer);
    }
    public void Deserialize(NetDataReader reader)
    {
        CharId = reader.GetInt();
        MapId = reader.GetInt();
        Template = new CharTemplatePacket();
        Template.Deserialize(reader);
    }
}