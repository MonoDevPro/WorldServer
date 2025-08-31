using LiteNetLib.Utils;
using Simulation.Application.DTOs;
using Simulation.Domain.Templates;

namespace Simulation.Networking.DTOs.Snapshots;

public struct EnterSnapshotPacket : INetSerializable
{
    public int MapId;
    public int CharId;
    public CharTemplatePacket[] Templates;

    public void FromDTO(in EnterSnapshot dto)
    {
        MapId = dto.mapId;
        CharId = dto.charId;
        Templates = new CharTemplatePacket[dto.templates.Length];
        for (int i = 0; i < dto.templates.Length; i++)
        {
            Templates[i] = new CharTemplatePacket();
            Templates[i].FromDTO(dto.templates[i]);
        }
    }
    
    public EnterSnapshot ToDTO() => new(MapId, CharId, Templates.Select(t => t.ToDTO()).ToArray());

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(MapId);
        writer.Put(CharId);
        writer.PutArray(Templates);
    }

    public void Deserialize(NetDataReader reader)
    {
        MapId = reader.GetInt();
        CharId = reader.GetInt();
        Templates = reader.GetArray<CharTemplatePacket>();
    }
}