using LiteNetLib;
using LiteNetLib.Utils;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Simulation.Core.Abstractions.Commons.VOs;
using Simulation.Core.Abstractions.Intents.In;
using Simulation.Core.Abstractions.Intents.Out;
using Simulation.Network;

namespace Simulation.Client;

// Adicionamos um enum para controlar o estado do cliente
enum ClientState { Connecting, Connected, InGame }

class Program
{
    private static volatile ClientState _state = ClientState.Connecting; // Estado inicial
    
    private static readonly NetPacketProcessor PacketProcessor = new NetPacketProcessor();

    private const int CharId = 1; 
    private static GameVector2 currentPosition;
    private static GameVector2 targetPosition;

    static void Main()
    {
        // --- Carrega a configuração ---
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        var networkOptions = new NetworkOptions();
        configuration.GetSection(NetworkOptions.SectionName).Bind(networkOptions);
        // --- Fim da seção de configuração ---
        
        var listener = new EventBasedNetListener();
        var client = new NetManager(listener) { UnsyncedEvents = false, IPv6Enabled = false };
        client.Start();
        var peer = client.Connect(networkOptions.ServerAddress, networkOptions.Port, networkOptions.ConnectionKey);

        Console.WriteLine("Cliente de Testes Iniciado.");
        Console.WriteLine("Tentando conectar ao servidor...");
        
        listener.PeerConnectedEvent += p =>
        {
            Console.WriteLine("Cliente de Testes Iniciado.");
            Console.WriteLine("Tentando conectar ao servidor {0}:{1}...", networkOptions.ServerAddress, networkOptions.Port);
            SendEnterGame(p, CharId);
            _state = ClientState.InGame; // CORREÇÃO: Altera o estado para InGame APÓS enviar o EnterGame
        };

        listener.PeerDisconnectedEvent += (p, info) => 
        {
            Console.WriteLine($"[DESCONECTADO] Motivo: {info.Reason}");
            _state = ClientState.Connecting; // Reseta o estado
        };

        listener.NetworkErrorEvent += (ep, err) => Console.WriteLine($"[ERRO DE REDE] {err}");

        listener.NetworkReceiveEvent += (fromPeer, dataReader, channel, deliveryMethod) =>
        {
            PacketProcessor.ReadAllPackets(dataReader, fromPeer);
            dataReader.Recycle();
        };
        
        PacketProcessor.SubscribeNetSerializable<MoveSnapshot>(HandleMoveSnapshot);
        PacketProcessor.SubscribeNetSerializable<AttackSnapshot>(HandleAttackSnapshot);

        Console.WriteLine("\n--- Controles ---");
        Console.WriteLine("C - Atacar");
        Console.WriteLine("W - Move Up");
        Console.WriteLine("A - Move Left");
        Console.WriteLine("S - Move Down");
        Console.WriteLine("D - Move Right");
        Console.WriteLine("Q - Sair");
        Console.WriteLine("-----------------\n");
        
        currentPosition = new GameVector2(10, 10);
        targetPosition = currentPosition;
        
        while (true)
        {
            client.PollEvents();

            // CORREÇÃO: Todas as ações agora só são permitidas se o estado for InGame
            if (_state == ClientState.InGame)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Q) break;
                    if (key == ConsoleKey.C)
                    {
                        Console.WriteLine("[AÇÃO] Enviando ataque...");
                        SendAttack(peer, CharId);
                    }
                    else if (key == ConsoleKey.W) targetPosition += new GameVector2(0, 1);
                    else if (key == ConsoleKey.S) targetPosition += new GameVector2(0, -1);
                    else if (key == ConsoleKey.A) targetPosition += new GameVector2(-1, 0);
                    else if (key == ConsoleKey.D) targetPosition += new GameVector2(1, 0);
                    
                    if (targetPosition != currentPosition)
                    {
                        var direction = targetPosition - currentPosition;
                        SendMove(peer, CharId, direction.X, direction.Y);
                        currentPosition = targetPosition; // Atualiza a posição atual para evitar múltiplos envios
                    }
                }
            }

            Thread.Sleep(15);
        }

        client.Stop();
        Console.WriteLine("Cliente finalizado.");
    }
    
    static void HandleMoveSnapshot(MoveSnapshot snapshot)
    {
        if (snapshot.CharId != CharId) return; // Ignora snapshots de outros personagens
        
        currentPosition = snapshot.Position;
        targetPosition = currentPosition; // Sincroniza a posição alvo com a atual para evitar
        
        Console.WriteLine($"[SNAPSHOT] Move -> Char:{snapshot.CharId} | Pos:({snapshot.Position.X},{snapshot.Position.Y}) | Dir:({snapshot.Direction.X},{snapshot.Direction.Y})");
    }
    static void HandleAttackSnapshot(AttackSnapshot snapshot)
    {
        Console.WriteLine($"[SNAPSHOT] Attack -> Char:{snapshot.CharId} iniciou um ataque!");
    }
    
    static void SendEnterGame(NetPeer peer, int charId)
    {
        var writer = new NetDataWriter();
        var intent = new EnterGameIntent(charId);
        PacketProcessor.WriteNetSerializable(writer, ref intent);
        peer.Send(writer, DeliveryMethod.ReliableOrdered);
        Console.WriteLine("[AÇÃO] Comando EnterGame enviado.");
    }
    
    static void SendMove(NetPeer peer, int charId, int dirX, int dirY)
    {
        var writer = new NetDataWriter();
        var direction = new GameVector2(dirX, dirY);
        var snapshot = new MoveIntent(charId, direction);
        PacketProcessor.WriteNetSerializable(writer, ref snapshot);
        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    static void SendAttack(NetPeer peer, int charId)
    {
        var writer = new NetDataWriter();
        var intent = new AttackIntent(charId);
        PacketProcessor.WriteNetSerializable(writer, ref intent);
        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }
}