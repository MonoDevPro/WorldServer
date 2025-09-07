using Godot;
using GameClient.Scripts.Infrastructure;
using GameClient.Scripts.Network.Packets;

namespace GameClient.Scripts.Network;

/// <summary>
/// Handles incoming snapshot packets from the server and publishes them to the local event bus
/// </summary>
public class PacketHandler
{
    private readonly IClientEventBus _localEventBus;

    public PacketHandler(IClientEventBus localEventBus)
    {
        _localEventBus = localEventBus;
    }

    public void Initialize()
    {
        // Note: In a real implementation, this would be called by the network layer
        // when packets are received. For this implementation, we'll assume the 
        // ClientNetworkApp handles packet routing to this handler.
    }

    public void HandleJoinAckSnapshot(ClientJoinAckSnapshot packet)
    {
        GD.Print($"Received JoinAckSnapshot: YourCharId={packet.YourCharId}, MapId={packet.MapId}, Others={packet.Others.Count}");
        _localEventBus.Publish(packet);
    }

    public void HandlePlayerJoinedSnapshot(ClientPlayerJoinedSnapshot packet)
    {
        GD.Print($"Received PlayerJoinedSnapshot: CharId={packet.NewPlayer.CharId}");
        _localEventBus.Publish(packet);
    }

    public void HandlePlayerLeftSnapshot(ClientPlayerLeftSnapshot packet)
    {
        GD.Print($"Received PlayerLeftSnapshot: CharId={packet.LeftPlayer.CharId}");
        _localEventBus.Publish(packet);
    }

    public void HandleMoveSnapshot(ClientMoveSnapshot packet)
    {
        GD.Print($"Received MoveSnapshot: CharId={packet.CharId}, Old=({packet.Old.X},{packet.Old.Y}), New=({packet.New.X},{packet.New.Y})");
        _localEventBus.Publish(packet);
    }

    public void HandleAttackSnapshot(ClientAttackSnapshot packet)
    {
        GD.Print($"Received AttackSnapshot: CharId={packet.CharId}");
        _localEventBus.Publish(packet);
    }

    public void HandleTeleportSnapshot(ClientTeleportSnapshot packet)
    {
        GD.Print($"Received TeleportSnapshot: CharId={packet.CharId}, MapId={packet.MapId}, Position=({packet.Position.X},{packet.Position.Y})");
        _localEventBus.Publish(packet);
    }
}