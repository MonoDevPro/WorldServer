using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using Simulation.Networking.DTOs.Intents;

namespace Simulation.Core.Tests;

/// <summary>
/// Framework simples de stress testing para validar performance e estabilidade
/// do servidor sob carga de múltiplos clientes simulados.
/// </summary>
public class StressTestFramework : IDisposable
{
    private readonly List<StressTestClient> _clients = new();
    private readonly ILogger<StressTestFramework> _logger;
    private readonly Random _random = new();
    private volatile bool _isRunning;

    public StressTestFramework(ILogger<StressTestFramework> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executa teste de stress com N clientes simulados
    /// </summary>
    public async Task RunStressTest(int clientCount, TimeSpan duration, string serverHost = "127.0.0.1", int serverPort = 9050)
    {
        _logger.LogInformation("Iniciando stress test com {ClientCount} clientes por {Duration}s", 
            clientCount, duration.TotalSeconds);

        var stopwatch = Stopwatch.StartNew();
        _isRunning = true;

        try
        {
            // Cria e conecta clientes
            for (int i = 0; i < clientCount; i++)
            {
                var client = new StressTestClient(i, _logger);
                _clients.Add(client);
                client.Connect(serverHost, serverPort);
                
                // Pequeno delay entre conexões para evitar spike
                await Task.Delay(50);
            }

            _logger.LogInformation("Todos {ClientCount} clientes conectados. Iniciando simulação...", clientCount);

            // Executa simulação por tempo determinado
            while (stopwatch.Elapsed < duration && _isRunning)
            {
                // Cada cliente faz ações aleatórias
                foreach (var client in _clients)
                {
                    if (_random.NextDouble() < 0.3) // 30% chance de ação por tick
                    {
                        client.PerformRandomAction();
                    }
                }

                await Task.Delay(16); // ~60 FPS
            }

            _logger.LogInformation("Stress test concluído. Coletando estatísticas...");
            await CollectStatistics();
        }
        finally
        {
            _isRunning = false;
            DisconnectAllClients();
        }
    }

    private Task CollectStatistics()
    {
        var connectedClients = _clients.Count(c => c.IsConnected);
        var totalPacketsSent = _clients.Sum(c => c.PacketsSent);
        var totalPacketsReceived = _clients.Sum(c => c.PacketsReceived);
        var averageLatency = _clients.Where(c => c.IsConnected).Average(c => c.LastLatencyMs);

        _logger.LogInformation(
            "Estatísticas do Stress Test:\n" +
            "- Clientes conectados: {ConnectedClients}/{TotalClients}\n" +
            "- Pacotes enviados: {PacketsSent}\n" +
            "- Pacotes recebidos: {PacketsReceived}\n" +
            "- Latência média: {AvgLatency}ms",
            connectedClients, _clients.Count,
            totalPacketsSent, totalPacketsReceived, averageLatency);

        // Força GC para medir pressão
        var memoryBefore = GC.GetTotalMemory(false);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var memoryAfter = GC.GetTotalMemory(true);

        _logger.LogInformation("Pressão de memória: {MemoryMB}MB antes do GC, {MemoryAfterMB}MB depois",
            memoryBefore / (1024.0 * 1024.0), memoryAfter / (1024.0 * 1024.0));
            
        return Task.CompletedTask;
    }

    private void DisconnectAllClients()
    {
        foreach (var client in _clients)
        {
            client.Dispose();
        }
        _clients.Clear();
    }

    public void Dispose()
    {
        _isRunning = false;
        DisconnectAllClients();
    }
}

/// <summary>
/// Cliente simulado para stress testing
/// </summary>
internal class StressTestClient : INetEventListener, IDisposable
{
    private readonly NetManager _netManager;
    private readonly NetPacketProcessor _packetProcessor;
    private readonly int _clientId;
    private readonly ILogger _logger;
    private readonly Random _random = new();
    private NetPeer? _serverPeer;
    private int _packetsSent;
    private int _packetsReceived;
    private int _lastLatencyMs;

    public bool IsConnected => _serverPeer?.ConnectionState == ConnectionState.Connected;
    public int PacketsSent => _packetsSent;
    public int PacketsReceived => _packetsReceived;
    public int LastLatencyMs => _lastLatencyMs;

    public StressTestClient(int clientId, ILogger logger)
    {
        _clientId = clientId;
        _logger = logger;
        _netManager = new NetManager(this);
        _packetProcessor = new NetPacketProcessor();
        
        RegisterPacketHandlers();
    }

    public void Connect(string host, int port)
    {
        _netManager.Start();
        _serverPeer = _netManager.Connect(host, port, "stress_test_client");
    }

    public void PerformRandomAction()
    {
        if (!IsConnected) return;

        try
        {
            // Ações aleatórias baseadas no protocolo real
            var actionType = _random.Next(4);
            switch (actionType)
            {
                case 0: // Movement
                    SendMoveIntent();
                    break;
                case 1: // Login attempt
                    SendLoginIntent();
                    break;
                case 2: // Attack
                    SendAttackIntent();
                    break;
                case 3: // Teleport
                    SendTeleportIntent();
                    break;
            }

            _packetsSent++;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Client {ClientId} erro ao enviar ação: {Error}", _clientId, ex.Message);
        }
    }

    private void SendMoveIntent()
    {
        var moveIntent = new MoveIntentPacket
        {
            CharId = _clientId,
            Input = new() { X = _random.Next(-1, 2), Y = _random.Next(-1, 2) }
        };
        
        var writer = new NetDataWriter();
        _packetProcessor.WriteNetSerializable(writer, ref moveIntent);
        _serverPeer?.Send(writer, DeliveryMethod.Unreliable);
    }

    private void SendLoginIntent()
    {
        var loginIntent = new EnterIntentPacket
        {
            CharId = _clientId
        };
        
        var writer = new NetDataWriter();
        _packetProcessor.WriteNetSerializable(writer, ref loginIntent);
        _serverPeer?.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    private void SendAttackIntent()
    {
        var attackIntent = new AttackIntentPacket
        {
            CharId = _clientId
        };
        
        var writer = new NetDataWriter();
        _packetProcessor.WriteNetSerializable(writer, ref attackIntent);
        _serverPeer?.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    private void SendTeleportIntent()
    {
        var teleportIntent = new TeleportIntentPacket
        {
            CharId = _clientId,
            MapId = 1, // Default map
            Position = new() { X = _random.Next(0, 100), Y = _random.Next(0, 100) }
        };
        
        var writer = new NetDataWriter();
        _packetProcessor.WriteNetSerializable(writer, ref teleportIntent);
        _serverPeer?.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    private void RegisterPacketHandlers()
    {
        // Handlers para snapshots recebidos - apenas contabiliza
        _packetProcessor.SubscribeNetSerializable<Simulation.Networking.DTOs.Snapshots.EnterSnapshotPacket>(OnSnapshotReceived);
        _packetProcessor.SubscribeNetSerializable<Simulation.Networking.DTOs.Snapshots.ExitSnapshotPacket>(OnSnapshotReceived);
        _packetProcessor.SubscribeNetSerializable<Simulation.Networking.DTOs.Snapshots.MoveSnapshotPacket>(OnSnapshotReceived);
        _packetProcessor.SubscribeNetSerializable<Simulation.Networking.DTOs.Snapshots.AttackSnapshotPacket>(OnSnapshotReceived);
        _packetProcessor.SubscribeNetSerializable<Simulation.Networking.DTOs.Snapshots.TeleportSnapshotPacket>(OnSnapshotReceived);
        _packetProcessor.SubscribeNetSerializable<Simulation.Networking.DTOs.Snapshots.CharSnapshotPacket>(OnSnapshotReceived);
    }

    private void OnSnapshotReceived<T>(T snapshot) where T : struct, INetSerializable
    {
        _packetsReceived++;
    }

    #region INetEventListener Implementation

    public void OnPeerConnected(NetPeer peer)
    {
        _logger.LogTrace("Cliente {ClientId} conectado", _clientId);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logger.LogTrace("Cliente {ClientId} desconectado: {Reason}", _clientId, disconnectInfo.Reason);
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        _logger.LogWarning("Cliente {ClientId} erro de rede: {Error}", _clientId, socketError);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        try
        {
            _packetProcessor.ReadAllPackets(reader);
        }
        finally
        {
            reader.Recycle();
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        reader.Recycle();
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        _lastLatencyMs = latency;
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        // Client não aceita conexões
    }

    #endregion

    public void Dispose()
    {
        _netManager?.Stop();
    }
}