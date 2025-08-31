using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Network;

/// <summary>
/// Implementação concreta do ISnapshotPublisher que usa LiteNetLib para enviar os snapshots pela rede.
/// </summary>
public class LiteNetPublisher(LiteNetServer server, ILogger<LiteNetPublisher> logger) : ISnapshotPublisher
{
    private readonly NetPacketProcessor _packetProcessor = new();
    private readonly NetDataWriter _writer = new();

    public void Publish(in EnterSnapshot snapshot)
    {
        if (server.TryGetPeer(snapshot.charId, out var peer) && peer != null)
        {
            _writer.Reset();
            var snap = snapshot;
            _packetProcessor.WriteNetSerializable(_writer, ref snap);
            peer.Send(_writer, DeliveryMethod.ReliableOrdered);
            logger.LogInformation("Snapshot de entrada enviado para CharId {CharId}", snapshot.charId);
        }
        else
        {
            logger.LogWarning("Não foi possível enviar EnterSnapshot: peer para CharId {CharId} não encontrado.", snapshot.charId);
        }
    }

    public void Publish(in CharSnapshot snapshot)
    {
        _writer.Reset();
        var snap = snapshot;
        _packetProcessor.WriteNetSerializable(_writer, ref snap);
        
        // Envia para todos, EXCETO para o próprio personagem do snapshot (ele já sabe de si mesmo)
        if (server.TryGetPeer(snapshot.CharId, out var excludePeer))
            server.Manager.SendToAll(_writer, DeliveryMethod.ReliableOrdered, excludePeer);
        else
            server.Manager.SendToAll(_writer, DeliveryMethod.ReliableOrdered);
        
        logger.LogInformation("Snapshot de personagem {CharId} transmitido para outros jogadores.", snapshot.CharId);
    }

    public void Publish(in ExitSnapshot snapshot)
    {
        _writer.Reset();
        var snap = snapshot;
        _packetProcessor.WriteNetSerializable(_writer, ref snap);
        server.Manager.SendToAll(_writer, DeliveryMethod.ReliableOrdered);
        logger.LogInformation("Snapshot de saída para CharId {CharId} transmitido.", snapshot.CharId);
    }

    public void Publish(in MoveSnapshot snapshot)
    {
        _writer.Reset();
        var snap = snapshot;
        _packetProcessor.WriteNetSerializable(_writer, ref snap);
        server.Manager.SendToAll(_writer, DeliveryMethod.Unreliable); // Movimento pode ser não confiável
        logger.LogTrace("Snapshot de movimento para CharId {CharId} transmitido.", snapshot.CharId);
    }

    public void Publish(in AttackSnapshot snapshot)
    {
        _writer.Reset();
        var snap = snapshot;
        _packetProcessor.WriteNetSerializable(_writer, ref snap);
        server.Manager.SendToAll(_writer, DeliveryMethod.ReliableOrdered);
        logger.LogInformation("Snapshot de ataque para CharId {CharId} transmitido.", snapshot.CharId);
    }
    
    public void Publish(in TeleportSnapshot snapshot)
    {
        _writer.Reset();
        var snap = snapshot;
        _packetProcessor.WriteNetSerializable(_writer, ref snap);
        server.Manager.SendToAll(_writer, DeliveryMethod.ReliableOrdered);
        logger.LogInformation("Snapshot de teleporte para CharId {CharId} transmitido.", snapshot.CharId);
    }

    public void Dispose()
    {
        // Nada a fazer aqui por enquanto
    }
}
