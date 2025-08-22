using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Abstractions.Out;
using Simulation.Core.Commons;
using Simulation.Core.Components;

namespace Simulation.Network;

/// <summary>
/// Servidor de rede básico com LiteNetLib que traduz mensagens para Requests da simulação.
/// Protocolo inicial simples para POC.
/// </summary>
public class LiteNetServer(ILogger<LiteNetServer> logger, ISimulationIntents intents, IEntityIndex entityIndex)
    : BackgroundService, INetEventListener
{
    private NetManager? _server;
    private readonly NetDataWriter _writer = new();

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _server = new NetManager(this)
        {
            UnsyncedEvents = true,
            IPv6Enabled = false
        };
        if (!_server.Start(27015))
        {
            logger.LogError("Failed to start LiteNetLib server on port 27015");
            return Task.CompletedTask;
        }
        logger.LogInformation("LiteNetLib server listening on 27015");

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
        _server?.Stop();
        return Task.CompletedTask;
    }

    // Events
    public void OnPeerConnected(NetPeer peer)
        => logger.LogInformation("Peer connected: {EndPoint}", peer.EndPoint);

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        => logger.LogInformation("Peer disconnected: {EndPoint} Reason: {Reason}", peer.EndPoint, disconnectInfo.Reason);

    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        => logger.LogWarning("Network error {Error} from {EndPoint}", socketError, endPoint);

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    { }

    public void OnConnectionRequest(ConnectionRequest request)
        => request.AcceptIfKey("worldserver-key");
    
    private void RegisterCustomTypes(NetPacketProcessor processor)
    {
        // Registra tipos personalizados, se necessário
        writer.Register<DirectionInput>();
        writer.Register<TilePosition>();
        writer.Register<GameVector2>();
    }
    

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        try
        {
            var type = reader.GetByte();
            switch (type)
            {
                case 1:
                {
                    // Move: [1][entityId:int][mapId:int][dirX:int][dirY:int]
                    var entityId = reader.GetInt();
                    var mapId = reader.GetInt();
                    var dirX = reader.GetInt();
                    var dirY = reader.GetInt();
                    if (entityIndex.TryGetByCharId(entityId, out var entity))
                    {
                        var req = new Requests.Move(entity, mapId, new DirectionInput { Direction = new VelocityVector(dirX, dirY) });
                        intents.EnqueueMove(req);
                    }
                    break;
                }
                case 2:
                {
                    // Teleport: [2][entityId:int][mapId:int][x:int][y:int]
                    var entityId = reader.GetInt();
                    var mapId = reader.GetInt();
                    var x = reader.GetInt();
                    var y = reader.GetInt();
                    if (entityIndex.TryGetByCharId(entityId, out var entity))
                    {
                        var req = new Requests.Teleport(entity, mapId, new TilePosition { Position = new GameVector2(x, y) });
                        intents.EnqueueTeleport(req);
                    }
                    break;
                }
                default:
                    logger.LogWarning("Unknown message type: {Type}", type);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing network message");
        }
        finally
        {
            reader.Recycle();
        }
    }

    public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        // Ignora mensagens unconnected neste POC
        reader.Recycle();
    }
}
