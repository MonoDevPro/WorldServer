using LiteNetLib.Utils;
using Simulation.Application.DTOs;
using Simulation.Domain.Components;

namespace Simulation.Networking.DTOs.Snapshots;

public struct MoveSnapshotPacket : INetSerializable
{
    public int CharId;
    public Position OldPosition;
    public Position NewPosition;
    
    public void FromDTO(in MoveSnapshot dto)
    {
        CharId = dto.CharId;
        OldPosition = dto.Old;
        NewPosition = dto.New;
    }

    public MoveSnapshot ToDTO() => new(CharId, OldPosition, NewPosition);

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CharId);
        writer.Put(OldPosition.X);
        writer.Put(OldPosition.Y);
        writer.Put(NewPosition.X);
        writer.Put(NewPosition.Y);
    }

    public void Deserialize(NetDataReader reader)
    {
        CharId = reader.GetInt();
        OldPosition = new Position { X = reader.GetInt(), Y = reader.GetInt() };
        NewPosition = new Position { X = reader.GetInt(), Y = reader.GetInt() };
    }
}