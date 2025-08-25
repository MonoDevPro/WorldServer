using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.Commons.VOs;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Abstractions.Intents.In;
using Simulation.Core.Abstractions.Intents.Out;
using Simulation.Core.Abstractions.Out;

namespace Simulation.Network;

public class LiteNetServer : BackgroundService, INetEventListener
{
    private readonly ILogger<LiteNetServer> _logger;
    private readonly IIntentProducer _intentProducer;
    private readonly ISnapshotEvents _snapshotEvents;
    private NetManager? _server;
    private readonly NetDataWriter _writer = new();

    public LiteNetServer(ILogger<LiteNetServer> logger, IEntityIndex entityIndex, IIntentProducer intentProducer, ISnapshotEvents snapshotEvents)
    {
        _logger = logger;
        _intentProducer = intentProducer;
        _snapshotEvents = snapshotEvents;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _server = new NetManager(this)
        {
            UnsyncedEvents = true,
            IPv6Enabled = false
        };
        
        _snapshotEvents.OnMoveSnapshot += SendMoveSnapshot;
        _snapshotEvents.OnAttackSnapshot += SendAttackSnapshot;

        if (!_server.Start(27015))
        {
            _logger.LogError("Falha ao iniciar o servidor LiteNetLib na porta 27015");
            return Task.CompletedTask;
        }
        _logger.LogInformation("Servidor LiteNetLib escutando na porta 27015");

        _ = Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _server.PollEvents();
                await Task.Delay(10, stoppingToken).ConfigureAwait(false);
            }
        }, stoppingToken);

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _snapshotEvents.OnMoveSnapshot -= SendMoveSnapshot;
        _snapshotEvents.OnAttackSnapshot -= SendAttackSnapshot;
        _server?.Stop();
        return base.StopAsync(cancellationToken);
    }
    
    public void OnPeerConnected(NetPeer peer) => _logger.LogInformation("Peer conectado: {EndPoint}", peer.EndPoint);

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logger.LogInformation("Peer desconectado: {EndPoint} Motivo: {Reason}", peer.EndPoint, disconnectInfo.Reason);
        
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
            var msgType = reader.GetByte();
            switch (msgType)
            {
                case 0: // EnterGame
                {
                    var charId = reader.GetInt();
                    peer.Tag = charId;
                    _logger.LogInformation("Recebido EnterGame. Enfileirando EnterGameIntent para o CharId: {CharId}", charId);
                    _intentProducer.EnqueueEnterGameIntent(new EnterGameIntent(charId));
                    break;
                }
                case 1: // Move
                {
                    var charId = reader.GetInt();
                    var dirX = reader.GetInt();
                    var dirY = reader.GetInt();
                    var input = new GameVector2(dirX, dirY);
                    _logger.LogDebug("Recebido Move. Enfileirando MoveIntent para o CharId: {CharId}, Direção: {Direction}", charId, input);
                    _intentProducer.EnqueueMoveIntent(new MoveIntent(charId, input));
                    break;
                }
                case 2: // Attack
                {
                    var charId = reader.GetInt();
                    _logger.LogInformation("Recebido Attack. Enfileirando AttackIntent para o CharId: {CharId}", charId);
                    _intentProducer.EnqueueAttackIntent(new AttackIntent(charId));
                    break;
                }
                default:
                    _logger.LogWarning("Tipo de mensagem desconhecido: {Type}", msgType);
                    break;
            }
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
        _writer.Put((byte)1);
        _writer.Put(snapshot.CharId);
        _writer.Put(snapshot.Position.X);
        _writer.Put(snapshot.Position.Y);
        _writer.Put(snapshot.Direction.X);
        _writer.Put(snapshot.Direction.Y);
        _server?.SendToAll(_writer, DeliveryMethod.Unreliable);
        _logger.LogTrace("Enviando MoveSnapshot para CharId: {CharId}", snapshot.CharId);
    }
    
    private void SendAttackSnapshot(AttackSnapshot snapshot)
    {
        _writer.Reset();
        _writer.Put((byte)2);
        _writer.Put(snapshot.CharId);
        _server?.SendToAll(_writer, DeliveryMethod.ReliableOrdered);
        _logger.LogInformation("Enviando AttackSnapshot para CharId: {CharId}", snapshot.CharId);
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) => reader.Recycle();
}