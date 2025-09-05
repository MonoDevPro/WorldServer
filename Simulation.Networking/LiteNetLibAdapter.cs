using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Application.DTOs.Intents;
using Simulation.Application.DTOs.Snapshots;
using Simulation.Application.Options;
using Simulation.Application.Ports.ECS.Handlers;
using Simulation.Application.Ports.ECS.Publishers;

namespace Simulation.Networking;

public class LiteNetLibAdapter : IPlayerSnapshotPublisher, IMapSnapshotPublisher, INetEventListener
{
    private readonly NetManager _netManager;
    private readonly IPlayerIntentHandler _intentHandler;
    private readonly NetDataWriter _writer = new();
    private readonly NetworkOptions _options;
    private readonly ILogger<LiteNetLibAdapter> _logger;

    // Mapeamento para saber qual conexão (NetPeer) pertence a qual jogador (CharId)
    private readonly ConcurrentDictionary<int, NetPeer> _charIdToPeer = new();
    private readonly ConcurrentDictionary<NetPeer, int> _peerToCharId = new();

    // Mapeamento para saber em qual mapa cada jogador está
    private readonly ConcurrentDictionary<int, int> _charIdToMapId = new();

    public LiteNetLibAdapter(IPlayerIntentHandler intentHandler, IOptions<NetworkOptions> options, ILogger<LiteNetLibAdapter> logger)
    {
        _intentHandler = intentHandler;
        _options = options.Value;
        _logger = logger;
        _netManager = new NetManager(this)
        {
            DisconnectTimeout = 10000
        };
    }

    public void Start()
    {
        _netManager.Start(_options.Port);
        Console.WriteLine($"Server started on port {_options.Port}");
    }

    public void Stop()
    {
        _netManager.Stop();
    }

    public void PollEvents()
    {
        _netManager.PollEvents();
    }

    //================================================================================
    // IPlayerSnapshotPublisher / IMapSnapshotPublisher Implementation (Saída)
    //================================================================================

    public void Publish(JoinAckDto joinAck)
    {
        if (_charIdToPeer.TryGetValue(joinAck.YourCharId, out var peer))
        {
            _writer.Reset();
            PacketProcessor.Write(_writer, joinAck);
            peer.Send(_writer, DeliveryMethod.ReliableOrdered);
        }
    }

    public void Publish(PlayerJoinedDto joined)
    {
        // Ao entrar, atualizamos o mapa do jogador
        _charIdToMapId[joined.NewPlayer.CharId] = joined.NewPlayer.MapId;
        
        _writer.Reset();
        PacketProcessor.Write(_writer, joined);
        // Envia para todos no mesmo mapa, exceto para o próprio jogador que entrou
        BroadcastToMap(joined.NewPlayer.MapId, _writer, joined.NewPlayer.CharId);
    }

    public void Publish(PlayerLeftDto left)
    {
        _writer.Reset();
        PacketProcessor.Write(_writer, left);
        // Envia para todos que estavam no mesmo mapa
        if(_charIdToMapId.TryGetValue(left.LeftPlayer.CharId, out var mapId))
        {
            BroadcastToMap(mapId, _writer, left.LeftPlayer.CharId);
        }
    }
    
    public void Publish(in MoveSnapshot s)
    {
        _writer.Reset();
        PacketProcessor.Write(_writer, s);
        if(_charIdToMapId.TryGetValue(s.CharId, out var mapId))
        {
            BroadcastToMap(mapId, _writer, s.CharId);
        }
    }
    
    public void Publish(in AttackSnapshot s)
    {
        _writer.Reset();
        PacketProcessor.Write(_writer, s);
        if(_charIdToMapId.TryGetValue(s.CharId, out var mapId))
        {
            BroadcastToMap(mapId, _writer, s.CharId);
        }
    }

    public void Publish(in TeleportSnapshot s)
    {
        // Ao teleportar, atualizamos o mapa do jogador
        _charIdToMapId[s.CharId] = s.MapId;

        _writer.Reset();
        PacketProcessor.Write(_writer, s);
        if (_charIdToPeer.TryGetValue(s.CharId, out var peer))
        {
            peer.Send(_writer, DeliveryMethod.ReliableOrdered);
        }
    }
    
    private static readonly Action<ILogger, int, Exception?> LogMapLoaded =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(1, nameof(LogMapLoaded)),
            "Mapa {MapId} carregado.");
    private static readonly Action<ILogger, int, Exception?> LogMapUnloaded =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(2, nameof(LogMapUnloaded)),
            "Mapa {MapId} descarregado.");
    
    public void Publish(in LoadMapSnapshot snapshot)
    {
        // Lógica para carregar o mapa. Geralmente não envolve pacotes de rede diretos
        // a menos que você queira notificar um serviço externo.
        LogMapLoaded(_logger, snapshot.MapId, null);
    }
    
    public void Publish(in UnloadMapSnapshot snapshot)
    {
        LogMapUnloaded(_logger, snapshot.MapId, null);
    }

    //================================================================================
    // INetEventListener Implementation (Entrada)
    //================================================================================

    public void OnPeerConnected(NetPeer peer)
    {
        Console.WriteLine($"Client connected: {peer.Id}");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Console.WriteLine($"Client disconnected: {peer.Id}. Reason: {disconnectInfo.Reason}");
        if (_peerToCharId.TryRemove(peer, out int charId))
        {
            _charIdToPeer.TryRemove(charId, out _);
            _charIdToMapId.TryRemove(charId, out _);
            _intentHandler.HandleIntent(new ExitIntent(charId));
        }
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        // A primeira intenção DEVE ser EnterIntent para associar o Peer ao CharId
        var initialPosition = reader.Position;
        var messageType = (MessageType)reader.GetByte();
        reader.SetPosition(initialPosition);

        if (messageType == MessageType.EnterIntent)
        {
            var charId = reader.GetByte(); // Pula o byte de tipo e lê o CharId
            _charIdToPeer[charId] = peer;
            _peerToCharId[peer] = charId;
            reader.SetPosition(initialPosition);
        }

        // Processa a intenção somente se o jogador já estiver registrado (associado)
        if (_peerToCharId.ContainsKey(peer))
        {
            PacketProcessor.ProcessIntent(reader, _intentHandler);
        }
        else
        {
            Console.WriteLine($"Received package from a non-identified peer: {peer.Id}. Package Discarded.");
        }
        
        reader.Recycle();
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { /* Log error */ }
    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { /* Ignorado para este caso */ }
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { /* Opcional */ }
    public void OnConnectionRequest(ConnectionRequest request)
    {
        // Opcional: Adicionar lógica de validação aqui (ex: checar uma chave de acesso)
        request.Accept();
    }

    //================================================================================
    // Métodos Auxiliares
    //================================================================================

    private void BroadcastToMap(int mapId, NetDataWriter writer, int excludeCharId = -1)
    {
        foreach (var entry in _charIdToMapId)
        {
            var charId = entry.Key;
            var playerMapId = entry.Value;

            if (charId != excludeCharId && playerMapId == mapId)
            {
                if (_charIdToPeer.TryGetValue(charId, out var peer))
                {
                    // Usar Unreliable para dados de alta frequência como movimento
                    var method = writer.Data[0] == (byte)MessageType.MoveSnapshot 
                        ? DeliveryMethod.Unreliable 
                        : DeliveryMethod.ReliableOrdered;

                    peer.Send(writer, method);
                }
            }
        }
    }

    public void Dispose()
    {
        Stop();
    }
}