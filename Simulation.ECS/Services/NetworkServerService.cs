using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Simulation.Application.Ports.ECS.Handlers;

// Este serviço é um Singleton que roda em background
namespace Simulation.ECS.Services;

public class NetworkServerService : IHostedService, INetEventListener
{
    private readonly ILogger<NetworkServerService> _logger;
    private readonly NetManager _netManager;
    private readonly PlayerLoginService _playerLoginService; // O serviço que criamos antes
    private readonly IPlayerStagingArea _playerStagingArea;

    // Mapeamento para encontrar a conexão de um jogador
    private readonly ConcurrentDictionary<int, NetPeer> _peersByCharId = new();

    public NetworkServerService(ILogger<NetworkServerService> logger, PlayerLoginService loginService, IPlayerStagingArea stagingArea)
    {
        _logger = logger;
        _playerLoginService = loginService;
        _playerStagingArea = stagingArea;
        _netManager = new NetManager(this);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _netManager.Start(9050); // Porta do servidor
        _logger.LogInformation("NetworkServer iniciado na porta {Port}.", 9050);

        // Inicia um loop para processar eventos de rede
        Task.Run(() =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _netManager.PollEvents();
                Thread.Sleep(15); // Evita uso de 100% da CPU
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("NetworkServer parando.");
        _netManager.Stop();
        return Task.CompletedTask;
    }
    
    // --- Implementação da INetEventListener ---

    public void OnPeerConnected(NetPeer peer)
    {
        _logger.LogInformation("Novo cliente conectado: {EndPoint}", peer.Id);
        // A lógica de autenticação e login começaria aqui.
        // Por exemplo, o cliente enviaria um pacote com um token e o CharId.
        // Por agora, vamos simular que o PlayerLoginService é chamado após a autenticação.
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logger.LogInformation("Cliente desconectado: {EndPoint}. Motivo: {Reason}", peer.Id, disconnectInfo.Reason);
        // Encontra o CharId associado a este 'peer' e o remove
        var item = _peersByCharId.FirstOrDefault(kvp => Equals(kvp.Value, peer));
        if (item.Key != 0)
        {
            _peersByCharId.TryRemove(item.Key, out _);
            // Enfileira a saída do jogador do mundo ECS
            _playerStagingArea.StageLeave(item.Key);
        }
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        _logger.LogError("Erro de rede em {EndPoint}: {Error}", endPoint, socketError);
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        // Pode ser usado para monitorar a latência dos jogadores
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        // Aceita todas as conexões (em um jogo real, haveria validação)
        request.AcceptIfKey("MySecretGameKey");
    }
    
    // O recebimento de dados é tratado aqui
    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        // Veja a seção 2 abaixo
        //var messageType = reader.GetByte();
        
        reader.Recycle();
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        // Pode ser usado para mensagens não conectadas, como pings
        reader.Recycle();
    }

    // ... outros métodos da interface (OnNetworkError, etc.)
}