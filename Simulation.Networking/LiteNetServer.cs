using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Application.DTOs;
using Simulation.Application.Options;
using Simulation.Application.Ports;
using Simulation.Application.Ports.Char;
using Simulation.Networking.DTOs.Intents;
using Simulation.Networking.DTOs.Snapshots;

namespace Simulation.Networking;

/// <summary>
/// Gerencia a instância do servidor LiteNetLib, atuando como a ponte principal
/// entre a rede e a simulação.
/// Implementa tanto a escuta de eventos de rede (INetEventListener)
/// quanto a publicação de snapshots (ISnapshotPublisher).
/// </summary>
public sealed class LiteNetServer : INetEventListener, ICharSnapshotPublisher
{
    private readonly NetManager _server;
    private readonly ICharIntentHandler _charIntentHandler;
    private readonly NetPacketProcessor _packetProcessor;
    private readonly ILogger<LiteNetServer> _logger;
    private readonly NetworkOptions _options;
    private readonly NetDataWriter _writer = new();
    
    // Mapeamentos para associações Peer <-> CharId
    private readonly ConcurrentDictionary<NetPeer, int> _peerToCharId = new();
    private readonly ConcurrentDictionary<int, NetPeer> _charIdToPeer = new();

    public LiteNetServer(
        ICharIntentHandler charIntentHandler, 
        NetPacketProcessor packetProcessor,
        IOptions<NetworkOptions> options,
        ILogger<LiteNetServer> logger)
    {
        _charIntentHandler = charIntentHandler;
        _packetProcessor = packetProcessor;
        _logger = logger;
        _options = options.Value;
        
        _server = new NetManager(this);
        RegisterIntentHandlers();
    }

    public void Start()
    {
        _server.Start(_options.Port);
        _logger.LogInformation("Servidor LiteNetLib iniciado na porta {Port}", _options.Port);
    }

    public void PollEvents() => _server.PollEvents();
    public void Stop() => _server.Stop();

    #region ISnapshotPublisher Implementation

    // --- Lógica de Publicação (Saída) ---
    public void Publish(in EnterSnapshot snapshot)
    {
        if (_charIdToPeer.TryGetValue(snapshot.charId, out var peer))
        {
            var packet = new EnterSnapshotPacket();
            packet.FromDTO(in snapshot);
            Send(peer, ref packet);
        }
    }

    public void Publish(in MoveSnapshot snapshot)
    {
        var packet = new MoveSnapshotPacket();
        packet.FromDTO(in snapshot);
        Broadcast(ref packet, DeliveryMethod.Unreliable);
    }

    public void Publish(in CharSnapshot snapshot)
    {
        var packet = new CharSnapshotPacket();
        packet.FromDTO(in snapshot);
        if (_charIdToPeer.TryGetValue(snapshot.CharId, out var excludePeer))
            BroadcastExcept(ref packet, excludePeer);
        else
            Broadcast(ref packet);
        
        _logger.LogInformation("Snapshot de personagem {CharId} transmitido para outros jogadores.", snapshot.CharId);
    }

    public void Publish(in ExitSnapshot snapshot)
    {
        var packet = new ExitSnapshotPacket();
        packet.FromDTO(in snapshot);
        if (_charIdToPeer.TryGetValue(snapshot.CharId, out var excludePeer))
            BroadcastExcept(ref packet, excludePeer);
        else
            Broadcast(ref packet);
        
        _logger.LogInformation("Snapshot de saída de personagem {CharId} transmitido para outros jogadores.", snapshot.CharId);
    }

    public void Publish(in AttackSnapshot snapshot)
    {
        var packet = new AttackSnapshotPacket();
        packet.FromDTO(in snapshot);
        if (_charIdToPeer.TryGetValue(snapshot.CharId, out var excludePeer))
            BroadcastExcept(ref packet, excludePeer);
        else
            Broadcast(ref packet);
        
        _logger.LogInformation("Snapshot de ataque de personagem {CharId} transmitido para outros jogadores.", snapshot.CharId);
    }
    public void Publish(in TeleportSnapshot snapshot)
    {
        var packet = new TeleportSnapshotPacket();
        packet.FromDTO(in snapshot);
        Broadcast(ref packet);
        
        _logger.LogInformation("Snapshot de teleporte de personagem {CharId} transmitido para todos os jogadores.", snapshot.CharId);
    }
    
    private void Send<T>(NetPeer peer, ref T packet, DeliveryMethod method = DeliveryMethod.ReliableOrdered) where T : struct, INetSerializable
    {
        _writer.Reset();
        _packetProcessor.WriteNetSerializable(_writer, ref packet);
        peer.Send(_writer, method);
    }
    private void BroadcastExcept<T>(ref T snapshot, NetPeer excludePeer, DeliveryMethod method = DeliveryMethod.ReliableOrdered) where T : struct, INetSerializable
    {
        _writer.Reset();
        var snap = snapshot;
        _packetProcessor.WriteNetSerializable(_writer, ref snap);
        _server.SendToAll(_writer, method, excludePeer);
        _logger.LogTrace("Snapshot {SnapshotType} transmitido para todos.", typeof(T).Name);
    }
    private void Broadcast<T>(ref T snapshot, DeliveryMethod method = DeliveryMethod.ReliableOrdered) where T : struct, INetSerializable
    {
        _writer.Reset();
        _packetProcessor.WriteNetSerializable(_writer, ref snapshot);
        _server.SendToAll(_writer, method);
        _logger.LogTrace("Snapshot {SnapshotType} transmitido para todos.", typeof(T).Name);
    }
    
    public void Dispose() => Stop();
    
    #endregion

    #region Intent & Event Handling

    private void RegisterIntentHandlers()
    {
        _packetProcessor.SubscribeNetSerializable<EnterIntentPacket, NetPeer>((intent, peer) =>
        {
            _logger.LogInformation("Recebido EnterIntent para CharId {CharId} do peer {PeerEndPoint}", intent.CharId, peer.Address);
            MapPeerToChar(peer, intent.CharId);
            _charIntentHandler.HandleIntent(intent.ToDTO());
        });

        // Delega a validação para um método genérico
        _packetProcessor.SubscribeNetSerializable<MoveIntentPacket, NetPeer>((intent, peer) => HandleAuthenticatedIntent(intent, peer, intent.CharId));
        _packetProcessor.SubscribeNetSerializable<AttackIntentPacket, NetPeer>((intent, peer) => HandleAuthenticatedIntent(intent, peer, intent.AttackerCharId));
        _packetProcessor.SubscribeNetSerializable<TeleportIntentPacket, NetPeer>((intent, peer) => HandleAuthenticatedIntent(intent, peer, intent.CharId));
        _packetProcessor.SubscribeNetSerializable<ExitIntentPacket, NetPeer>((intent, peer) => HandleAuthenticatedIntent(intent, peer, intent.CharId, true));
    }

    private void HandleAuthenticatedIntent<T>(in T intent, NetPeer peer, int intentCharId, bool isExit = false) where T : struct
    {
        if (!TryValidatePeer(peer, intentCharId)) return;

        // Passa o intent para o ECS
        if (intent is EnterIntentPacket enter) _charIntentHandler.HandleIntent(enter.ToDTO());
        else if (intent is ExitIntentPacket exit) _charIntentHandler.HandleIntent(exit.ToDTO());
        else if (intent is MoveIntentPacket move) _charIntentHandler.HandleIntent(move.ToDTO());
        else if (intent is AttackIntentPacket attack) _charIntentHandler.HandleIntent(attack.ToDTO());
        else if (intent is TeleportIntentPacket teleport) _charIntentHandler.HandleIntent(teleport.ToDTO());

        // Se for uma intenção de saída, desassocia o peer
        if (isExit)
        {
            UnmapPeer(peer);
        }
    }
    
    private bool TryValidatePeer(NetPeer peer, int expectedCharId)
    {
        if (!_peerToCharId.TryGetValue(peer, out var mappedCharId))
        {
            _logger.LogWarning("Intent recebido de peer não autenticado {PeerEndPoint}. Ignorando.", peer.Address);
            return false;
        }
        if (mappedCharId != expectedCharId)
        {
            _logger.LogWarning("Peer {PeerEndPoint} (CharId {Mapped}) tentou enviar intent para CharId {Expected}. Ignorando.", peer.Address, mappedCharId, expectedCharId);
            return false;
        }
        return true;
    }

    private void MapPeerToChar(NetPeer peer, int charId)
    {
        UnmapPeer(peer); // Garante que o peer não está associado a nenhum CharId antigo
        if (_charIdToPeer.TryGetValue(charId, out var oldPeer))
        {
            UnmapPeer(oldPeer); // Garante que o CharId não está associado a nenhum peer antigo (ex: reconexão)
        }
        
        _peerToCharId[peer] = charId;
        _charIdToPeer[charId] = peer;
    }
    
    private void UnmapPeer(NetPeer peer)
    {
        if (_peerToCharId.TryRemove(peer, out var charId))
        {
            _charIdToPeer.TryRemove(charId, out _);
        }
    }

    #endregion
    
    #region INetEventListener Implementation
    
    public void OnPeerConnected(NetPeer peer) => _logger.LogInformation("Peer conectado: {PeerEndPoint}", peer.Address);
    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logger.LogInformation("Peer desconectado: {PeerEndPoint}. Motivo: {Reason}", peer.Address, disconnectInfo.Reason);
        if (_peerToCharId.TryGetValue(peer, out var charId))
        {
            _charIntentHandler.HandleIntent(new ExitIntent(charId));
            UnmapPeer(peer);
        }
    }
    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        try
        {
            _packetProcessor.ReadAllPackets(reader, peer);
        }
        finally
        {
            reader.Recycle();
        }
    }
    public void OnConnectionRequest(ConnectionRequest request) => request.AcceptIfKey(_options.ConnectionKey);
    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) => _logger.LogError("Erro de rede de {EndPoint}: {Error}", endPoint, socketError);
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { /* Opcional */ }
    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { reader.Recycle(); }
    
    #endregion
}