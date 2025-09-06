using GameClient.Scripts.Domain;
using GameClient.Scripts.DTOs;
using LiteNetLib;
using LiteNetLib.Utils;

namespace GameClient.Scripts.Networking;

public static class PacketProcessor
{
    public static object? ReadSnapshot(NetPacketReader reader)
    {
        var type = (MessageType)reader.GetByte();
        switch (type)
        {
            case MessageType.JoinAck:
            {
                var yourCharId = reader.GetInt();
                var yourEntityId = reader.GetInt();
                var mapId = reader.GetInt();
                var count = reader.GetInt();
                var list = new List<PlayerStateDto>(count);
                for (int i=0;i<count;i++) list.Add(ReadPlayerStateDto(reader));
                return new JoinAckDto(yourCharId, yourEntityId, mapId, list);
            }
            case MessageType.PlayerJoined:
                return new PlayerJoinedDto(ReadPlayerStateDto(reader));
            case MessageType.PlayerLeft:
                return new PlayerLeftDto(ReadPlayerStateDto(reader));
            case MessageType.MoveSnapshot:
            {
                var charId = reader.GetInt();
                var old = new Position{ X = reader.GetInt(), Y = reader.GetInt() };
                var @new = new Position{ X = reader.GetInt(), Y = reader.GetInt() };
                return new MoveSnapshot(charId, old, @new);
            }
            case MessageType.AttackSnapshot:
            {
                var charId = reader.GetInt();
                return new AttackSnapshot(charId);
            }
            case MessageType.TeleportSnapshot:
            {
                var charId = reader.GetInt();
                var mapId = reader.GetInt();
                var pos = new Position{ X = reader.GetInt(), Y = reader.GetInt() };
                return new TeleportSnapshot(charId, mapId, pos);
            }
        }
        return null;
    }

    public static void WriteEnterIntent(NetDataWriter writer, int charId)
    {
        writer.Put((byte)MessageType.EnterIntent);
        writer.Put(charId);
    }

    public static void WriteExitIntent(NetDataWriter writer, int charId)
    {
        writer.Put((byte)MessageType.ExitIntent);
        writer.Put(charId);
    }

    public static void WriteMoveIntent(NetDataWriter writer, int charId, int x, int y)
    {
        writer.Put((byte)MessageType.MoveIntent);
        writer.Put(charId);
        writer.Put(x); writer.Put(y);
    }

    public static void WriteAttackIntent(NetDataWriter writer, int charId)
    {
        writer.Put((byte)MessageType.AttackIntent);
        writer.Put(charId);
    }

    public static void WriteTeleportIntent(NetDataWriter writer, int charId, int mapId, Position pos)
    {
        writer.Put((byte)MessageType.TeleportIntent);
        writer.Put(charId);
        writer.Put(mapId);
        writer.Put(pos.X); writer.Put(pos.Y);
    }

    private static PlayerStateDto ReadPlayerStateDto(NetPacketReader reader)
    {
        return new PlayerStateDto(
            CharId: reader.GetInt(),
            EntityId: reader.GetInt(),
            MapId: reader.GetInt(),
            Position: new Position{ X = reader.GetInt(), Y = reader.GetInt() },
            Direction: new Direction{ X = reader.GetInt(), Y = reader.GetInt() },
            MoveSpeed: reader.GetFloat(),
            AttackCastTime: reader.GetFloat(),
            AttackCooldown: reader.GetFloat());
    }
}
