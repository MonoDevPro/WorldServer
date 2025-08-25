using Arch.Core;
using Arch.System;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.Commons.VOs;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Abstractions.Intents.In;
using Simulation.Core.Abstractions.Intents.Out;
using Simulation.Core.Abstractions.Out;

namespace Simulation.Network;

public class NetworkSystem(
    ILogger<NetworkSystem> logger,
    IIntentProducer intentProducer,
    ISnapshotEvents snapshotEvents,
    World world)
    : BaseSystem<World, float>(world), INetEventListener
{
    private NetManager? _server;
    private readonly NetDataWriter _writer = new();
    
    private bool _started;
    
    public override void Update(in float delta)
    {
        if (!_started) return;
        
        _server?.PollEvents();
        
        logger.LogTrace("NetworkSystem PollEvents chamado.");
    }

    public bool Start()
    {
        if (_started)
        {
            logger.LogDebug("LiteNetServer já iniciado (Start chamado novamente). Ignorando.");
            return true;
        }
        
        _server = new NetManager(this)
        {
            UnsyncedEvents = false,
            IPv6Enabled = false
        };
        
        snapshotEvents.OnMoveSnapshot += SendMoveSnapshot;
        snapshotEvents.OnAttackSnapshot += SendAttackSnapshot;
        
        if (!_server.Start(27015))
        {
            logger.LogError("Falha ao iniciar o servidor LiteNetLib na porta {Port}", 27015);
            _server = null;
            return false;
        }

        logger.LogInformation("Servidor LiteNetLib escutando na porta {Port}", 27015);
        _started = true;
        return true;
    }
    
    public void Stop()
    {
        if (!_started) return;
        
        snapshotEvents.OnMoveSnapshot -= SendMoveSnapshot;
        snapshotEvents.OnAttackSnapshot -= SendAttackSnapshot;
        _server?.Stop();
        
        _server = null;
        _started = false;
    }
    
    public void OnPeerConnected(NetPeer peer) => logger.LogInformation("Peer conectado: {EndPoint}", peer.EndPoint);

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        logger.LogInformation("Peer desconectado: {EndPoint} Motivo: {Reason}", peer.EndPoint, disconnectInfo.Reason);
        
        if (peer.Tag is int charId)
        {
            logger.LogInformation("Enfileirando ExitGameIntent para o CharId: {CharId}", charId);
            intentProducer.EnqueueExitGameIntent(new ExitGameIntent(charId));
        }
    }

    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        => logger.LogWarning("Erro de rede {Error} de {EndPoint}", socketError, endPoint);
    
    public void OnConnectionRequest(ConnectionRequest request) => request.AcceptIfKey("worldserver-key");

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        try
        {
            var msgType = reader.GetByte();
            switch (msgType)
            {
                case 0: // EnterGame
                {
                    var charId = reader.GetInt();
                    peer.Tag = charId;
                    logger.LogInformation("Recebido EnterGame. Enfileirando EnterGameIntent para o CharId: {CharId}", charId);
                    intentProducer.EnqueueEnterGameIntent(new EnterGameIntent(charId));
                    break;
                }
                case 1: // Move
                {
                    var charId = reader.GetInt();
                    var dirX = reader.GetInt();
                    var dirY = reader.GetInt();
                    var input = new GameVector2(dirX, dirY);
                    logger.LogDebug("Recebido Move. Enfileirando MoveIntent para o CharId: {CharId}, Direção: {Direction}", charId, input);
                    intentProducer.EnqueueMoveIntent(new MoveIntent(charId, input));
                    break;
                }
                case 2: // Attack
                {
                    var charId = reader.GetInt();
                    logger.LogInformation("Recebido Attack. Enfileirando AttackIntent para o CharId: {CharId}", charId);
                    intentProducer.EnqueueAttackIntent(new AttackIntent(charId));
                    break;
                }
                default:
                    logger.LogWarning("Tipo de mensagem desconhecido: {Type}", msgType);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar mensagem de rede");
        }
        finally
        {
            reader.Recycle();
        }
    }

    private void SendMoveSnapshot(MoveSnapshot snapshot)
    {
        _writer.Reset();
        _writer.Put((byte)1);
        _writer.Put(snapshot.CharId);
        _writer.Put(snapshot.Position.X);
        _writer.Put(snapshot.Position.Y);
        _writer.Put(snapshot.Direction.X);
        _writer.Put(snapshot.Direction.Y);
        _server?.SendToAll(_writer, DeliveryMethod.Unreliable);
        logger.LogTrace("Enviando MoveSnapshot para CharId: {CharId}", snapshot.CharId);
    }
    
    private void SendAttackSnapshot(AttackSnapshot snapshot)
    {
        _writer.Reset();
        _writer.Put((byte)2);
        _writer.Put(snapshot.CharId);
        _server?.SendToAll(_writer, DeliveryMethod.ReliableOrdered);
        logger.LogInformation("Enviando AttackSnapshot para CharId: {CharId}", snapshot.CharId);
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) => reader.Recycle();
}