using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Ports;
using Simulation.Core.Abstractions.Ports.Char;
using Simulation.Core.Abstractions.Ports.Index;

namespace Simulation.Network;

/// <summary>
/// Gerencia a instância do servidor LiteNetLib e seus eventos de baixo nível.
/// Atua como a ponte principal entre a rede e o resto da aplicação.
/// </summary>
public class LiteNetServer : INetEventListener
{
    private readonly NetManager _server;
    private readonly IIntentHandler _intentHandler;
    private readonly NetPacketProcessor _packetProcessor;
    private readonly ILogger<LiteNetServer> _logger;
    private readonly NetworkOptions _options;
    
    // Mapeamento para saber qual CharId está associado a qual NetPeer
    private readonly ConcurrentDictionary<NetPeer, int> _peerToCharId = new();

    public NetManager Manager => _server;

    public LiteNetServer(
        IIntentHandler intentHandler, 
        NetPacketProcessor packetProcessor,
        IOptions<NetworkOptions> options,
        ILogger<LiteNetServer> logger)
    {
        _intentHandler = intentHandler;
        _packetProcessor = packetProcessor;
        _logger = logger;
        _options = options.Value;
        
        _server = new NetManager(this)
        {
            EnableStatistics = true,
            AllowPeerAddressChange = true
        };

        // Registra os handlers para os intents que vêm da rede
        RegisterIntentHandlers();
    }

    public void Start()
    {
        _server.Start(_options.Port);
        _logger.LogInformation("Servidor LiteNetLib iniciado na porta {Port}", _options.Port);
    }

    public void PollEvents() => _server.PollEvents();
    public void Stop() => _server.Stop();
    
    // Mapeia um CharId a uma conexão de peer
    public void MapPeerToChar(NetPeer peer, int charId) => _peerToCharId[peer] = charId;
    public bool TryGetPeer(int charId, out NetPeer? peer)
    {
        // Esta busca pode ser lenta se houver muitos jogadores.
        // Para otimizar, mantenha um dicionário reverso CharId -> NetPeer.
        var pair = _peerToCharId.FirstOrDefault(p => p.Value == charId);
        if (pair.Key != null)
        {
            peer = pair.Key;
            return true;
        }
        peer = null;
        return false;
    }

    private void RegisterIntentHandlers()
    {
        _packetProcessor.SubscribeNetSerializable<EnterIntent, NetPeer>((intent, peer) =>
        {
            // O EnterIntent é especial: é ele quem estabelece a associação Peer <-> CharId.
            _logger.LogInformation("Recebido EnterIntent para CharId {CharId} do peer {PeerEndPoint}", intent.CharId, peer.Address);
            MapPeerToChar(peer, intent.CharId);
            _intentHandler.HandleIntent(in intent);
        });
        
        // Para todos os outros intents, verificamos se o peer já está autenticado (tem um CharId)
        _packetProcessor.SubscribeNetSerializable<MoveIntent, NetPeer>((intent, peer) => HandleAuthenticatedIntent(intent, peer, () => _intentHandler.HandleIntent(intent)));
        _packetProcessor.SubscribeNetSerializable<AttackIntent, NetPeer>((intent, peer) => HandleAuthenticatedIntent(intent, peer, () => _intentHandler.HandleIntent(intent)));
        _packetProcessor.SubscribeNetSerializable<TeleportIntent, NetPeer>((intent, peer) => HandleAuthenticatedIntent(intent, peer, () => _intentHandler.HandleIntent(intent)));
    }
    
    private void HandleAuthenticatedIntent<T>(T intent, NetPeer peer, Delegate process) where T : struct
    {
        if (!_peerToCharId.ContainsKey(peer))
        {
            _logger.LogWarning("Intent {IntentType} recebido de um peer não autenticado {PeerEndPoint}. Ignorando.", typeof(T).Name, peer.Address);
            return;
        }
        process.DynamicInvoke();
    }

    #region INetEventListener Implementation
    
    public void OnPeerConnected(NetPeer peer) => _logger.LogInformation("Peer conectado: {PeerEndPoint}", peer.Address);

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logger.LogInformation("Peer desconectado: {PeerEndPoint}. Motivo: {Reason}", peer.Address, disconnectInfo.Reason);
        if (_peerToCharId.TryRemove(peer, out var charId))
        {
            // Notifica o ECS que este personagem saiu do jogo
            _intentHandler.HandleIntent(new ExitIntent(charId));
        }
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        _packetProcessor.ReadAllPackets(reader, peer);
        reader.Recycle();
    }
    
    public void OnConnectionRequest(ConnectionRequest request) => request.AcceptIfKey(_options.ConnectionKey);
    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) => _logger.LogError("Erro de rede de {EndPoint}: {Error}", endPoint, socketError);
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { /* Opcional: registrar latência */ }
    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
    
    #endregion
}
