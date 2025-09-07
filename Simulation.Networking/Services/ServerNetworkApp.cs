using Simulation.Application.Options;
using Simulation.Application.Ports.Network.Domain.Events;
using Simulation.Application.Ports.Network.Inbound;
using Simulation.Application.Ports.Network.Outbound;

namespace Simulation.Networking.Services;

public class ServerApp : IServerNetworkApp
{
    private readonly IServerNetworkService _serverNetworkService;

    public NetworkOptions Options { get; }
    public IConnectionManager ConnectionManager { get; }
    public IPacketSender PacketSender { get; }
    public IPacketRegistry PacketRegistry { get;}
    public INetworkEventBus EventBus { get; }

    public ServerApp(
        IServerNetworkService networkService,
        NetworkOptions config,
        IConnectionManager connectionManager,
        IPacketSender packetSender,
        IPacketRegistry packetRegistry,
        INetworkEventBus eventBus)
    {
        _serverNetworkService = networkService;
        Options = config;
        ConnectionManager = connectionManager;
        PacketSender = packetSender;
        PacketRegistry = packetRegistry;
        EventBus = eventBus;

        EventBus.Subscribe<ConnectionRequestEvent>(ProcessConnectionRequest);
    }

    private void ProcessConnectionRequest(ConnectionRequestEvent connectionRequest)
    {
        // Processar solicitação de conexão
        // Exemplo: Verificar se o cliente pode se conectar com X Ip e Y connectionKey
        // Se não puder, rejeitar a conexão

        var ip = connectionRequest.EventArgs.RequestInfo.RemoteEndPoint;
        var connectionKey = connectionRequest.EventArgs.RequestInfo.ConnectionKey;
        
        // Podemos facilmente processar uma blacklist de IPs ou verificar se o cliente já está conectado
        // Também podemos delegar isso para um serviço de autenticação.

        if (connectionKey == Options.ConnectionKey)
            // Aceitar a conexão
            connectionRequest.EventArgs.ShouldAccept = true;
        else
            // Rejeitar a conexão
            connectionRequest.EventArgs.ShouldAccept = false;
    }

    public bool Start()
    {
        return _serverNetworkService.Start(Options.ServerPort);
    }

    public void Stop()
    {
        _serverNetworkService.Stop();
    }

    public void Update(float deltaTime)
    {
        _serverNetworkService.Update();
    }

    public void DisconnectPeer(int peerId)
    {
        _serverNetworkService.DisconnectPeer(peerId);
    }
    
    public void Dispose()
    {
        _serverNetworkService.Stop();
        EventBus.Unsubscribe<ConnectionRequestEvent>(ProcessConnectionRequest);
    }
}
