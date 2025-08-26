using LiteNetLib;
using LiteNetLib.Utils;
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

    // network
    private static NetManager? _client;
    private static EventBasedNetListener? _listener;
    private static NetPeer? _peer;
    private static readonly NetPacketProcessor PacketProcessor = new NetPacketProcessor();

    // player
    private const int CharId = 1;

    // authoritative / predicted positions
    private static GameVector2 currentPosFromServer = new GameVector2(10, 10);
    private static GameVector2 predictedPos = currentPosFromServer;
    
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

        // network init
        _listener = new EventBasedNetListener();
        _client = new NetManager(_listener) { UnsyncedEvents = false, IPv6Enabled = false };
        _client.Start();
        _peer = _client.Connect(networkOptions.ServerAddress, networkOptions.Port, networkOptions.ConnectionKey);

        Console.WriteLine("Cliente de Testes Iniciado.");
        Console.WriteLine("Tentando conectar ao servidor...");

        _listener.PeerConnectedEvent += p =>
        {
            Console.WriteLine($"Conectado ao servidor {networkOptions.ServerAddress}:{networkOptions.Port}");
            SendEnterGame(p, CharId);
            _state = ClientState.InGame;
            _peer = p;
        };

        _listener.PeerDisconnectedEvent += (p, info) =>
        {
            Console.WriteLine($"[DESCONECTADO] Motivo: {info.Reason}");
            _state = ClientState.Connecting;
            currentPosFromServer = new GameVector2(10, 10);
            predictedPos = currentPosFromServer;
        };

        _listener.NetworkErrorEvent += (ep, err) => Console.WriteLine($"[ERRO DE REDE] {err}");

        _listener.NetworkReceiveEvent += (fromPeer, dataReader, channel, deliveryMethod) =>
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

        // main loop
        while (true)
        {
            _client.PollEvents();

            if (_state == ClientState.InGame)
            {
                // keyboard input
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Q) break;

                    if (key == ConsoleKey.C)
                    {
                        Console.WriteLine("[AÇÃO] Enviando ataque...");
                        SendAttack(_peer!, CharId);
                        continue;
                    }

                    GameVector2 dir;
                    if (key == ConsoleKey.W) dir = new GameVector2(0, -1); // Y -1 = cima
                    else if (key == ConsoleKey.S) dir = new GameVector2(0, 1);
                    else if (key == ConsoleKey.A) dir = new GameVector2(-1, 0);
                    else if (key == ConsoleKey.D) dir = new GameVector2(1, 0);
                    else continue;

                    // create input with seq and send (unreliable sequenced)
                    var input = new MoveIntent(CharId, dir); // <- adapte se seu ctor for diferente
                    SendMove(input);
                    
                    // apply locally for immediate feedback (prediction)
                    predictedPos += dir; // velocidade fixa 1 unidade grid por input
                    
                    Console.WriteLine($"[PREDICTION] Nova posição predita: {predictedPos.X},{predictedPos.Y}");
                }
            }

            Thread.Sleep(10);
        }


        _client.Stop();
        Console.WriteLine("Cliente finalizado.");
    }

    static void HandleMoveSnapshot(MoveSnapshot snapshot)
    {
        if (snapshot.CharId != CharId) 
            return;

        // authoritative position from server
        currentPosFromServer = snapshot.NewPosition;
        
        // Simple reconciliation: if the predicted position diverges from the server's authoritative position, correct it
        if (predictedPos != currentPosFromServer)
        {
            Console.WriteLine($"[RECONCILIATION] Posicao corrigida do servidor: ({currentPosFromServer.X},{currentPosFromServer.Y})");
            predictedPos = currentPosFromServer;
        }
        else
            Console.WriteLine($"[SNAPSHOT] Posicao confirmada pelo servidor: ({currentPosFromServer.X},{currentPosFromServer.Y})");
    }

    static void HandleAttackSnapshot(AttackSnapshot snapshot)
    {
        Console.WriteLine($"[SNAPSHOT] Attack -> Char:{snapshot.CharId} iniciou um ataque!");
    }

    static void SendEnterGame(NetPeer peer, int charId)
    {
        if (_peer == null) return;
        var intent = new EnterGameIntent(charId);
        Send(_peer, intent, DeliveryMethod.ReliableOrdered);
        Console.WriteLine("[AÇÃO] Comando EnterGame enviado.");
    }

    static void SendMove(MoveIntent input)
    {
        if (_peer == null) return;
        Send(_peer, input, DeliveryMethod.ReliableOrdered);
        Console.WriteLine($"[AÇÃO] Comando Move enviado. Direção: ({input.Input.X},{input.Input.Y})");
    }

    static void SendAttack(NetPeer peer, int charId)
    {
        var intent = new AttackIntent(charId);
        Send(peer, intent, DeliveryMethod.ReliableOrdered);
        Console.WriteLine("[AÇÃO] Comando Attack enviado.");
    }

    static void Send<T>(NetPeer peer, T intent, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
        where T : struct, INetSerializable
    {
        var writer = new NetDataWriter();
        PacketProcessor.WriteNetSerializable(writer, ref intent);
        peer.Send(writer, method);
    }
}