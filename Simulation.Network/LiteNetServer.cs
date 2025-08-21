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
public class LiteNetServer : BackgroundService, INetEventListener
{
    private readonly ILogger<LiteNetServer> _logger;
    private readonly ISimulationRequests _requests;
    private NetManager? _server;
    private readonly NetDataWriter _writer = new();

    private readonly IEntityIndex _entityIndex;

    public LiteNetServer(ILogger<LiteNetServer> logger, ISimulationRequests requests, IEntityIndex entityIndex)
    {
        _logger = logger;
        _requests = requests;
        _entityIndex = entityIndex;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _server = new NetManager(this)
        {
            UnsyncedEvents = true,
            IPv6Enabled = false
        };
        if (!_server.Start(27015))
        {
            _logger.LogError("Failed to start LiteNetLib server on port 27015");
            return Task.CompletedTask;
        }
        _logger.LogInformation("LiteNetLib server listening on 27015");

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
        => _logger.LogInformation("Peer connected: {EndPoint}", peer.EndPoint);

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        => _logger.LogInformation("Peer disconnected: {EndPoint} Reason: {Reason}", peer.EndPoint, disconnectInfo.Reason);

    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        => _logger.LogWarning("Network error {Error} from {EndPoint}", socketError, endPoint);

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    { }

    public void OnConnectionRequest(ConnectionRequest request)
        => request.AcceptIfKey("worldserver-key");

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
                    if (_entityIndex.TryGetByCharId(entityId, out var entity))
                    {
                        var req = new Requests.Move(entity, mapId, new DirectionInput { Direction = new VelocityVector(dirX, dirY) });
                        _requests.EnqueueMove(req);
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
                    if (_entityIndex.TryGetByCharId(entityId, out var entity))
                    {
                        var req = new Requests.Teleport(entity, mapId, new TilePosition { Position = new GameVector2(x, y) });
                        _requests.EnqueueTeleport(req);
                    }
                    break;
                }
                default:
                    _logger.LogWarning("Unknown message type: {Type}", type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing network message");
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
