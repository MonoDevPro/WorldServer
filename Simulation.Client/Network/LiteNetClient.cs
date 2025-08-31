using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Client.Core;
using System.Net;
using System.Net.Sockets;
using Simulation.Application.DTOs;
using Simulation.Application.Options;
using Simulation.Networking.DTOs.Intents;
using Simulation.Networking.DTOs.Snapshots;

namespace Simulation.Client.Network;

/// <summary>
/// Cliente LiteNetLib que conecta ao servidor e gerencia a comunicação.
/// Implementa IIntentSender para enviar comandos e usa ISnapshotHandler para processar snapshots.
/// </summary>
public class LiteNetClient : INetEventListener, IIntentSender, IDisposable
{
    private readonly NetManager _client;
    private readonly NetPacketProcessor _packetProcessor;
    private readonly ISnapshotHandler _snapshotHandler;
    private readonly NetworkOptions _options;
    private readonly ILogger<LiteNetClient> _logger;
    private readonly NetDataWriter _writer = new();
    
    private NetPeer? _serverPeer;
    private bool _disposed;

    public LiteNetClient(
        ISnapshotHandler snapshotHandler,
        IOptions<NetworkOptions> options,
        ILogger<LiteNetClient> logger)
    {
        _snapshotHandler = snapshotHandler;
        _options = options.Value;
        _logger = logger;
        _packetProcessor = new NetPacketProcessor();
        
        _client = new NetManager(this)
        {
            EnableStatistics = true
        };
        
        RegisterSnapshotHandlers();
    }

    public bool IsConnected => _serverPeer?.ConnectionState == ConnectionState.Connected;

    public void Connect()
    {
        _logger.LogInformation("Conectando ao servidor {ServerAddress}:{Port}", _options.ServerAddress, _options.Port);
        _client.Start();
        _client.Connect(_options.ServerAddress, _options.Port, _options.ConnectionKey);
    }

    public void Disconnect()
    {
        _logger.LogInformation("Desconectando do servidor...");
        _serverPeer?.Disconnect();
        _serverPeer = null;
    }

    public void PollEvents() => _client.PollEvents();

    private void RegisterSnapshotHandlers()
    {
        _packetProcessor.SubscribeNetSerializable<EnterSnapshotPacket>((snapshot) => _snapshotHandler.HandleSnapshot(snapshot.ToDTO()));
        _packetProcessor.SubscribeNetSerializable<CharSnapshotPacket>((snapshot) => _snapshotHandler.HandleSnapshot(snapshot.ToDTO()));
        _packetProcessor.SubscribeNetSerializable<ExitSnapshotPacket>((snapshot) => _snapshotHandler.HandleSnapshot(snapshot.ToDTO()));
        _packetProcessor.SubscribeNetSerializable<MoveSnapshotPacket>((snapshot) => _snapshotHandler.HandleSnapshot(snapshot.ToDTO()));
        _packetProcessor.SubscribeNetSerializable<AttackSnapshotPacket>((snapshot) => _snapshotHandler.HandleSnapshot(snapshot.ToDTO()));
        _packetProcessor.SubscribeNetSerializable<TeleportSnapshotPacket>((snapshot) => _snapshotHandler.HandleSnapshot(snapshot.ToDTO()));
    }

    // IIntentSender implementation
    public void SendIntent(in EnterIntentPacket intent) => SendIntentToServer(intent);
    public void SendIntent(in ExitIntentPacket intent) => SendIntentToServer(intent);
    public void SendIntent(in MoveIntentPacket intent) => SendIntentToServer(intent);
    public void SendIntent(in AttackIntentPacket intent) => SendIntentToServer(intent);
    public void SendIntent(in TeleportIntentPacket intent) => SendIntentToServer(intent);

    private void SendIntentToServer<T>(in T intent) where T : struct, INetSerializable
    {
        if (_serverPeer?.ConnectionState != ConnectionState.Connected)
        {
            _logger.LogWarning("Tentativa de enviar intent {IntentType} sem conexão ativa", typeof(T).Name);
            return;
        }

        _writer.Reset();
        var intentCopy = intent;
        _packetProcessor.WriteNetSerializable(_writer, ref intentCopy);
        _serverPeer.Send(_writer, DeliveryMethod.ReliableOrdered);
        _logger.LogTrace("Intent {IntentType} enviado para o servidor", typeof(T).Name);
    }

    #region INetEventListener Implementation
    
    public void OnPeerConnected(NetPeer peer)
    {
        _serverPeer = peer;
        _logger.LogInformation("Conectado ao servidor: {ServerEndPoint}", peer.Address);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logger.LogInformation("Desconectado do servidor: {ServerEndPoint}. Motivo: {Reason}", 
            peer.Address, disconnectInfo.Reason);
        _serverPeer = null;
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        _packetProcessor.ReadAllPackets(reader, peer);
        reader.Recycle();
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        _logger.LogError("Erro de rede de {EndPoint}: {Error}", endPoint, socketError);
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        // Cliente não aceita conexões
        request.Reject();
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        // Opcional: registrar latência
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        // Cliente não processa mensagens não conectadas
        reader.Recycle();
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        
        Disconnect();
        _client?.Stop();
        _disposed = true;
    }
}