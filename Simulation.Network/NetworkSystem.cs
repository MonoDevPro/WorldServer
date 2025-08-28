using System.Net;
using System.Net.Sockets;
using Arch.Core;
using Arch.System;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Adapters.Data;
using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Network;

public class NetworkSystem : BaseSystem<World, float>, INetEventListener
{
    private NetManager? _server;
    private readonly NetDataWriter _writer = new();
    private readonly NetPacketProcessor _processor = new();
    private readonly NetworkOptions _options;
    
    private bool _started;
    private readonly ILogger<NetworkSystem> _logger;
    private readonly IIntentHandler _intentProducer;
    private readonly ISnapshotPublisher _snapshotEvents;
    
    // Test -> isso vai vir do db futuramente.
    private readonly ILifecycleSystem _lifecycleSystem;
    
    private readonly Dictionary<NetPeer, int> _charIdsByPeer = new();
    private readonly Dictionary<int, NetPeer> _peersByCharId = new();

    public NetworkSystem(ILogger<NetworkSystem> logger,
        IIntentHandler intentProducer,
        ISnapshotPublisher snapshotEvents,
        IOptions<NetworkOptions> options,
        ILifecycleSystem lifecycleSystem,
        World world) : base(world)
    {
        _logger = logger;
        _intentProducer = intentProducer;
        _snapshotEvents = snapshotEvents;
        _options = options.Value;
        _lifecycleSystem = lifecycleSystem;
        
        // --- REGISTRO DE HANDLERS USANDO INTERCEPTORES ---

        // 1. EnterGame: Qualquer peer conectado pode enviar.
        _processor.SubscribeNetSerializable<EnterGameIntent, NetPeer>((intent, peer) =>
        {
            if (_charIdsByPeer.ContainsKey(peer))
            {
                _logger.LogWarning("Peer {EndPoint} já autenticado. Ignorando novo EnterGameIntent.", peer.Address);
                return;
            }
            
            var charTemplate = new CharTemplate
            {
                CharId = new CharId(intent.CharacterId),
                MapId = new MapId(1), // Default map
                Position = new Position { Value = new GameCoord(0, 0) }, // Default position
                Direction = new Direction { Value = new GameDirection(0, 0) }, // Default direction
                MoveStats = new MoveStats { Speed = 1f },
                AttackStats = new AttackStats { CastTime = 1f, Cooldown = 1f },
                Blocking = new Blocking()
            };
            
            _lifecycleSystem.EnqueueSpawn(charTemplate);
            
            _charIdsByPeer[peer] = intent.CharacterId;
            _peersByCharId[intent.CharacterId] = peer;
            peer.Tag = intent.CharacterId;
            intentProducer.EnqueueEnterGameIntent(intent);
            
            _logger.LogInformation("Peer {EndPoint} autenticado com CharId: {CharId}", peer.Address, intent.CharacterId);
        });

        // 2. Ações em jogo: Apenas peers que já entraram no jogo (autenticados).
        RegisterAuthenticatedIntent<MoveIntent>((intent) => _intentProducer.EnqueueMoveIntent(intent));
        RegisterAuthenticatedIntent<AttackIntent>( (intent) => _intentProducer.EnqueueAttackIntent(intent));

        // --- REGISTRO DE EVENTOS DE SAÍDA (SNAPSHOTS) ---
        _snapshotEvents.OnEnterGameSnapshot += SendGameSnapshot;
        _snapshotEvents.OnCharExitSnapshot += SendCharCharExitSnapshot;
        _snapshotEvents.OnMoveSnapshot += SendMoveSnapshot;
        _snapshotEvents.OnAttackSnapshot += SendAttackSnapshot;
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
        
        _snapshotEvents.OnEnterGameSnapshot -= SendGameSnapshot;
        _snapshotEvents.OnCharExitSnapshot -= SendCharCharExitSnapshot;
        _snapshotEvents.OnMoveSnapshot -= SendMoveSnapshot;
        _snapshotEvents.OnAttackSnapshot -= SendAttackSnapshot;
        _server?.Stop();
        
        _server = null;
        _started = false;
    }
    
    public void OnPeerConnected(NetPeer peer) => _logger.LogInformation("Peer conectado: {EndPoint}", peer.Address);

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logger.LogInformation("Peer desconectado: {EndPoint} Motivo: {Reason}", peer.Address, disconnectInfo.Reason);
        
        if (peer.Tag is int charId)
        {
            _logger.LogInformation("Enfileirando ExitGameIntent para o CharId: {CharId}", charId);
            _intentProducer.EnqueueExitGameIntent(new ExitGameIntent(charId));
            _charIdsByPeer.Remove(peer);
            _peersByCharId.Remove(charId);
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
            if (!_charIdsByPeer.ContainsKey(peer))
            {
                _logger.LogWarning("Intent {IntentType} recebido de um peer não autenticado. Ignorando.", typeof(T).Name);
                return;
            }
            onReceive(intent);
        });
    }
    
    private void SendGameSnapshot(EnterSnapshot snapshot)
    {
        _writer.Reset();
        _processor.WriteNetSerializable(_writer, ref snapshot);
        _peersByCharId[snapshot.currentCharId].Send(_writer, DeliveryMethod.ReliableOrdered);
        _logger.LogInformation("Enviando GameSnapshot para CharId: {CharId}", snapshot.currentCharId);
        
        int currentCharId = snapshot.currentCharId;
        CharTemplate? charSnapshot = snapshot.AllEntities.FirstOrDefault(cs => cs.CharId.Value == currentCharId);
        _writer.Reset();
        _processor.WriteNetSerializable(_writer, ref charSnapshot);
        _server?.SendToAll(_writer, DeliveryMethod.ReliableOrdered, _peersByCharId[snapshot.currentCharId]);
        _logger.LogInformation("Enviando CharSnapshot para todos: {CharId}", snapshot.currentCharId);
        
    }
    
    private void SendCharCharExitSnapshot(ExitSnapshot snapshot)
    {
        _writer.Reset();
        _processor.WriteNetSerializable(_writer, ref snapshot);
        _server?.SendToAll(_writer, DeliveryMethod.ReliableOrdered);
        _logger.LogInformation("Enviando CharExitSnapshot para CharId: {CharId}", snapshot.CharId);
    }

    private void SendMoveSnapshot(MoveSnapshot snapshot)
    {
        _writer.Reset();
        _processor.WriteNetSerializable(_writer, ref snapshot);
        _server?.SendToAll(_writer, DeliveryMethod.ReliableOrdered);
        _logger.LogTrace("Enviando MoveSnapshot para CharId: {CharId}", snapshot.CharId);
    }
    
    private void SendAttackSnapshot(AttackSnapshot snapshot)
    {
        _writer.Reset();
        _processor.WriteNetSerializable(_writer, ref snapshot);
        _server?.SendToAll(_writer, DeliveryMethod.ReliableOrdered);
        _logger.LogInformation("Enviando AttackSnapshot para CharId: {CharId}", snapshot.CharId);
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) => reader.Recycle();
}