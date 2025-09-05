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
        MapId = dto.MapId;
        CharId = dto.CharId;
        Templates = new CharTemplatePacket[dto.templates.Length];
        for (int i = 0; i < dto.templates.Length; i++)
        {
            Templates[i] = new CharTemplatePacket();
            Templates[i].FromDTO(dto.templates[i]);
        }
    }
    
    public EnterSnapshot ToDTO()
    {
        // Use for loop instead of LINQ to avoid allocation
        var templateArray = new CharTemplate[Templates.Length];
        for (int i = 0; i < Templates.Length; i++)
        {
            templateArray[i] = Templates[i].ToDTO();
        }
        return new EnterSnapshot(MapId, CharId, templateArray);
    }

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