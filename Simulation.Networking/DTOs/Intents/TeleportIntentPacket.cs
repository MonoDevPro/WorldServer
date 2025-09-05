using LiteNetLib.Utils;
using Simulation.Application.DTOs;
using Simulation.Domain.Components;

namespace Simulation.Networking.DTOs.Intents;

public struct TeleportIntentPacket : INetSerializable
{
    public int CharId;
    public int MapId;
    public Position Position;

    public void FromDTO(in TeleportIntent dto)
    {
        CharId = dto.CharId;
        MapId = dto.MapId;
        Position = dto.Pos;
    }

    public Application.DTOs.TeleportIntent ToDTO() => new(CharId, MapId, Position);

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CharId);
        writer.Put(MapId);
        writer.Put(Position.X);
        writer.Put(Position.Y);
    }

    public void Deserialize(NetDataReader reader)
    {
        CharId = reader.GetInt();
        MapId = reader.GetInt();
        Position = new Position { X = reader.GetInt(), Y = reader.GetInt() };
    }
}