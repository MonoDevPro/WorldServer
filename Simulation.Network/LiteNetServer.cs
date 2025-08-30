using System.Net;
using System.Net.Sockets;
using Arch.Core;
using Arch.System;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Adapters.Char;
using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Network;

public class NetworkSystem : BaseSystem<World, float>, INetEventListener
{
    private NetManager? _server;
    private readonly NetPacketProcessor _processor = new();
    private readonly NetworkOptions _options;
    
    private bool _started;
    private readonly ILogger<NetworkSystem> _logger;
    private readonly IIntentHandler _intentProducer;
    private readonly ISnapshotPublisher _snapshotEvents;
    
    
    public NetworkSystem(ILogger<NetworkSystem> logger,
        IIntentHandler intentProducer,
        ISnapshotPublisher snapshotEvents,
        IOptions<NetworkOptions> options,
        World world) : base(world)
    {
        _logger = logger;
        _intentProducer = intentProducer;
        _snapshotEvents = snapshotEvents;
        _options = options.Value;
        
        // --- REGISTRO DE HANDLERS USANDO INTERCEPTORES ---

        // 1. EnterGame: Qualquer peer conectado pode enviar.
        _processor.SubscribeNetSerializable<EnterGameIntent, NetPeer>((intent, peer) =>
        {
            if (peer.Tag is not null)
            {
                _logger.LogWarning("Peer {EndPoint} já autenticado. Ignorando novo EnterGameIntent.", peer.Address);
                return;
            }
            
            var charTemplate = new CharTemplate
            {
                Name = "Player" + intent.CharId,
                Gender = Gender.Male,
                Vocation = Vocation.Mage,
                CharId = intent.CharId,
                MapId = 1, // Default map
                Position = new GameCoord(0, 0), // Default position
                Direction = new GameDirection(0, 1), // Default direction south
                MoveSpeed = 1.0f,
                AttackCastTime = 1.0f,
                AttackCooldown = 1.0f,
            };
            
            intentProducer.EnqueueEnterGameIntent(intent, charTemplate);
            peer.Tag = intent.CharId;
            intentProducer.EnqueueEnterGameIntent(intent, charTemplate);
            
            _logger.LogInformation("Peer {EndPoint} autenticado com CharId: {CharId}", peer.Address, intent.CharId);
        });

        // 2. Ações em jogo: Apenas peers que já entraram no jogo (autenticados).
        RegisterAuthenticatedIntent<MoveIntent>((intent) => _intentProducer.EnqueueMoveIntent(intent));
        RegisterAuthenticatedIntent<AttackIntent>( (intent) => _intentProducer.EnqueueAttackIntent(intent));
    }

    public override void Update(in float delta)
    {
        if (!_started) return;
        
        _server?.PollEvents();
    }

    public bool Start()
    {
        if (_started)
        {
            _logger.LogDebug("LiteNetServer já iniciado (Start chamado novamente). Ignorando.");
            return true;
        }
        
        _server = new NetManager(this)
        {
            UnsyncedEvents = false,
            IPv6Enabled = false
        };
        
        if (!_server.Start(_options.Port))
        {
            _logger.LogError("Falha ao iniciar o servidor LiteNetLib na porta {Port}", _options.Port);
            _server = null;
            return false;
        }

        _logger.LogInformation("Servidor LiteNetLib escutando na porta {Port}", _options.Port);
        _started = true;
        return true;
    }
    
    public void Stop()
    {
        if (!_started) return;
        
        _server?.Stop();
        
        _server = null;
        _started = false;
    }
    
    public void OnPeerConnected(NetPeer peer) => _logger.LogInformation("Peer conectado: {EndPoint}", peer.Address);

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logger.LogInformation("Peer desconectado: {EndPoint} Motivo: {Reason}", peer.Address, disconnectInfo.Reason);
        
        if (!_peersByCharId.ContainsValue(peer)) return;
        {
            _logger.LogInformation("Enfileirando ExitGameIntent para o CharId: {CharId}", charId);
            _intentProducer.EnqueueExitGameIntent(new ExitGameIntent(charId));
            peer.Tag = null;
        }
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        => _logger.LogWarning("Erro de rede {Error} de {EndPoint}", socketError, endPoint);
    
    public void OnConnectionRequest(ConnectionRequest request) => request.AcceptIfKey(_options.ConnectionKey);

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        try
        {
            _processor.ReadAllPackets(reader, peer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem de rede");
        }
        finally
        {
            reader.Recycle();
        }
    }
    
    /// <summary>
    /// Interceptador genérico que valida se um peer está autenticado (em jogo)
    /// antes de processar o comando.
    /// </summary>
    private void RegisterAuthenticatedIntent<T>(Action<T> onReceive) where T : INetSerializable, new()
    {
        _processor.SubscribeNetSerializable<T, NetPeer>((intent, peer) =>
        {
            if (peer.Tag is null)
            {
                _logger.LogWarning("Peer {EndPoint} não autenticado. Ignorando {IntentType}.", peer.Address, typeof(T).Name);
                return;
            }
            
            onReceive(intent);
        });
    }
    
    private void SendGameSnapshot(EnterSnapshot snapshot)
    {
        
        
    }
    
    private void SendCharCharExitSnapshot(ExitSnapshot snapshot)
    {
        
    }

    private void SendMoveSnapshot(MoveSnapshot snapshot)
    {
        
    }
    
    private void SendAttackSnapshot(AttackSnapshot snapshot)
    {
        
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) => reader.Recycle();
    public NetPeer? GetPeerByCharId(int charId)
    {
        return _peersByCharId.GetValueOrDefault(charId);
    }
}