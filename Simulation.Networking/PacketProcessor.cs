using LiteNetLib;
using LiteNetLib.Utils;
using Simulation.Application.DTOs;
using Simulation.Application.DTOs.Intents;
using Simulation.Application.DTOs.Snapshots;
using Simulation.Application.Ports.ECS.Handlers;
using Simulation.Domain.Components;

namespace Simulation.Networking;

// Classe auxiliar para serializar e desserializar os DTOs e Intenções.
public static class PacketProcessor
{
    //================================================================================
    // INTENTS (CLIENT -> SERVER)
    //================================================================================

    public static void ProcessIntent(NetPacketReader reader, IPlayerIntentHandler handler)
    {
        var messageType = (MessageType)reader.GetByte();
        switch (messageType)
        {
            case MessageType.EnterIntent:
                {
                    var intent = new EnterIntent(reader.GetInt());
                    var state = ReadPlayerStateDto(reader); // Cliente envia seu estado inicial
                    handler.HandleIntent(intent, state);
                    break;
                }
            case MessageType.ExitIntent:
                {
                    handler.HandleIntent(new ExitIntent(reader.GetInt()));
                    break;
                }
            case MessageType.AttackIntent:
                {
                    handler.HandleIntent(new AttackIntent(reader.GetInt()));
                    break;
                }
            case MessageType.MoveIntent:
                {
                    handler.HandleIntent(new MoveIntent(reader.GetInt(),  new Input{ X = reader.GetInt(), Y = reader.GetInt() } ));
                    break;
                }
            case MessageType.TeleportIntent:
                {
                    handler.HandleIntent(new TeleportIntent(
                        CharId: reader.GetInt(),
                        MapId: reader.GetInt(),
                        Pos: new Position { X = reader.GetInt(), Y = reader.GetInt() }
                    ));
                    break;
                }
        }
    }

    //================================================================================
    // SNAPSHOTS (SERVER -> CLIENT)
    //================================================================================

    public static void Write(NetDataWriter writer, JoinAckDto dto)
    {
        writer.Put((byte)MessageType.JoinAck);
        writer.Put(dto.YourCharId);
        writer.Put(dto.YourEntityId);
        writer.Put(dto.MapId);
        writer.Put(dto.Others.Count);
        foreach (var other in dto.Others)
        {
            WritePlayerStateDto(writer, other);
        }
    }

    public static void Write(NetDataWriter writer, PlayerJoinedDto dto)
    {
        writer.Put((byte)MessageType.PlayerJoined);
        WritePlayerStateDto(writer, dto.NewPlayer);
    }

    public static void Write(NetDataWriter writer, PlayerLeftDto dto)
    {
        writer.Put((byte)MessageType.PlayerLeft);
        WritePlayerStateDto(writer, dto.LeftPlayer);
    }
    
    public static void Write(NetDataWriter writer, in MoveSnapshot s)
    {
        writer.Put((byte)MessageType.MoveSnapshot);
        writer.Put(s.CharId);
        writer.Put(s.Old.X); writer.Put(s.Old.Y);
        writer.Put(s.New.X); writer.Put(s.New.Y);
    }

    public static void Write(NetDataWriter writer, in AttackSnapshot s)
    {
        writer.Put((byte)MessageType.AttackSnapshot);
        writer.Put(s.CharId);
    }

    public static void Write(NetDataWriter writer, in TeleportSnapshot s)
    {
        writer.Put((byte)MessageType.TeleportSnapshot);
        writer.Put(s.CharId);
        writer.Put(s.MapId);
        writer.Put(s.Position.X); writer.Put(s.Position.Y);
    }

    // Helpers
    private static void WritePlayerStateDto(NetDataWriter writer, PlayerStateDto state)
    {
        writer.Put(state.CharId);
        writer.Put(state.EntityId);
        writer.Put(state.MapId);
        writer.Put(state.Position.X); writer.Put(state.Position.Y);
        writer.Put(state.Direction.X); writer.Put(state.Direction.Y);
        writer.Put(state.MoveSpeed);
        writer.Put(state.AttackCastTime);
        writer.Put(state.AttackCooldown);
    }

    private static PlayerStateDto ReadPlayerStateDto(NetPacketReader reader)
    {
        return new PlayerStateDto(
            CharId: reader.GetInt(),
            EntityId: reader.GetInt(),
            MapId: reader.GetInt(),
            Position: new Position { X = reader.GetInt(), Y = reader.GetInt() },
            Direction: new Direction { X = reader.GetInt(), Y = reader.GetInt() },
            MoveSpeed: reader.GetFloat(),
            AttackCastTime: reader.GetFloat(),
            AttackCooldown: reader.GetFloat()
        );
    }
}