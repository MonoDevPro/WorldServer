using Arch.Core;
using Arch.System;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Abstractions.Intents.In;
using Simulation.Core.Abstractions.Intents.Out;
using Simulation.Core.Abstractions.Out;

namespace Simulation.Network;

public class NetworkSystem : BaseSystem<World, float>, INetEventListener
{
    private NetManager? _server;
    private readonly NetDataWriter _writer = new();
    private readonly NetPacketProcessor _processor = new();
    
    private bool _started;
    private readonly ILogger<NetworkSystem> _logger;
    private readonly IIntentProducer _intentProducer;
    private readonly ISnapshotEvents _snapshotEvents;

    public NetworkSystem(ILogger<NetworkSystem> logger,
        IIntentProducer intentProducer,
        ISnapshotEvents snapshotEvents,
        World world) : base(world)
    {
        _logger = logger;
        _intentProducer = intentProducer;
        _snapshotEvents = snapshotEvents;
        
        // Registrar handlers - SubscribeReusable evita alocações
        _processor.SubscribeNetSerializable<EnterGameIntent, NetPeer>((intent, peer) =>
        {
            peer.Tag = intent.CharacterId;
            intentProducer.EnqueueEnterGameIntent(intent);
        });
        _processor.SubscribeNetSerializable<MoveIntent, NetPeer>((intent, peer) =>
        {
            if (peer.Tag == null)
            {
                logger.LogWarning("MoveIntent recebido de um peer não autenticado. Ignorando.");
                return;
            }
            intentProducer.EnqueueMoveIntent(intent);
        });
        _processor.SubscribeNetSerializable<AttackIntent, NetPeer>((intent, peer) =>
        {
            if (peer.Tag == null)
            {
                logger.LogWarning("AttackIntent recebido de um peer não autenticado. Ignorando.");
                return;
            }
            intentProducer.EnqueueAttackIntent(intent);
        });
        
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
        
        
        
        if (!_server.Start(27015))
        {
            _logger.LogError("Falha ao iniciar o servidor LiteNetLib na porta {Port}", 27015);
            _server = null;
            return false;
        }

        _logger.LogInformation("Servidor LiteNetLib escutando na porta {Port}", 27015);
        _started = true;
        return true;
    }
    
    public void Stop()
    {
        if (!_started) return;
        
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
        }
    }

    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        => _logger.LogWarning("Erro de rede {Error} de {EndPoint}", socketError, endPoint);
    
    public void OnConnectionRequest(ConnectionRequest request) => request.AcceptIfKey("worldserver-key");

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
    public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) => reader.Recycle();
}