using Simulation.Application.Ports.Network.Domain.Models;
using Simulation.Application.Ports.Network.Outbound;
using Simulation.Domain.Components;

namespace Simulation.Networking.Packets;

// S -> C
public class JoinAckSnapshotPacket : IPacket, ISerializable
{
    public int YourCharId { get; set; }
    public int MapId { get; set; }
    public List<PlayerStateDto> Others { get; set; } = new();

    public void Serialize(INetworkWriter writer)
    {
        writer.WriteInt(YourCharId);
        writer.WriteInt(MapId);
        writer.WriteInt(Others.Count);
        foreach (var player in Others)
        {
            player.Serialize(writer);
        }
    }

    public void Deserialize(INetworkReader reader)
    {
        YourCharId = reader.ReadInt();
        MapId = reader.ReadInt();
        int count = reader.ReadInt();
        Others = new List<PlayerStateDto>(count);
        for (int i = 0; i < count; i++)
        {
            var player = new PlayerStateDto();
            player.Deserialize(reader);
            Others.Add(player);
        }
    }
}

// S -> C
public class PlayerJoinedSnapshotPacket : IPacket, ISerializable
{
    public PlayerStateDto NewPlayer { get; set; } = new();

    public void Serialize(INetworkWriter writer) => NewPlayer.Serialize(writer);
    public void Deserialize(INetworkReader reader) => NewPlayer.Deserialize(reader);
}

// S -> C
public class PlayerLeftSnapshotPacket : IPacket, ISerializable
{
    public PlayerStateDto LeftPlayer { get; set; } = new();

    public void Serialize(INetworkWriter writer) => LeftPlayer.Serialize(writer);
    public void Deserialize(INetworkReader reader) => LeftPlayer.Deserialize(reader);
}
    
// S -> C
public class AttackSnapshotPacket : IPacket, ISerializable
{
    public int CharId { get; set; }

    public void Serialize(INetworkWriter writer) => writer.WriteInt(CharId);
    public void Deserialize(INetworkReader reader) => CharId = reader.ReadInt();
}
    
// S -> C
public class MoveSnapshotPacket : IPacket, ISerializable
{
    public int CharId { get; set; }
    public Position Old { get; set; }
    public Position New { get; set; }

    public void Serialize(INetworkWriter writer)
    {
        writer.WriteInt(CharId);
        writer.WriteInt(Old.X);
        writer.WriteInt(Old.Y);
        writer.WriteInt(New.X);
        writer.WriteInt(New.Y);
    }

    public void Deserialize(INetworkReader reader)
    {
        CharId = reader.ReadInt();
        Old = new Position { X = reader.ReadInt(), Y = reader.ReadInt() };
        New = new Position { X = reader.ReadInt(), Y = reader.ReadInt() };
    }
}
    
// S -> C
public class TeleportSnapshotPacket : IPacket, ISerializable
{
    public int CharId { get; set; }
    public int MapId { get; set; }
    public Position Position { get; set; }

    public void Serialize(INetworkWriter writer)
    {
        writer.WriteInt(CharId);
        writer.WriteInt(MapId);
        writer.WriteInt(Position.X);
        writer.WriteInt(Position.Y);
    }

    public void Deserialize(INetworkReader reader)
    {
        CharId = reader.ReadInt();
        MapId = reader.ReadInt();
        Position = new Position { X = reader.ReadInt(), Y = reader.ReadInt() };
    }
}