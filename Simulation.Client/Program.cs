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
    
    // Adicione um dicionário para guardar os outros jogadores
    private static readonly Dictionary<int, GameVector2> OtherPlayers = new();

    // player
    private static int CharId = -1;

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
            
            Console.Write($"Digite seu CharId (número inteiro): ");
            var input = Console.ReadLine();
            if (!int.TryParse(input, out CharId))
            {
                CharId = -1; // Valor inválido
            }
            
            if (CharId <= 0)
            {
                Console.WriteLine("CharId inválido. Encerrando cliente.");
                _client.Stop();
                Environment.Exit(0);
            }
            
            SendEnterGame(p, CharId);
            _peer = p;
        };

        _listener.PeerDisconnectedEvent += (p, info) =>
        {
            Console.WriteLine($"[DESCONECTADO] Motivo: {info.Reason}");
            _state = ClientState.Connecting;
            currentPosFromServer = new GameVector2(10, 10);
            predictedPos = currentPosFromServer;
            OtherPlayers.Clear();
            CharId = -1;
        };

        _listener.NetworkErrorEvent += (ep, err) => Console.WriteLine($"[ERRO DE REDE] {err}");

        _listener.NetworkReceiveEvent += (fromPeer, dataReader, channel, deliveryMethod) =>
        {
            PacketProcessor.ReadAllPackets(dataReader, fromPeer);
            dataReader.Recycle();
        };

        PacketProcessor.SubscribeNetSerializable<GameSnapshot>(HandleGameSnapshot); 
        PacketProcessor.SubscribeNetSerializable<CharSnapshot>(HandleCharSnapshot);
        PacketProcessor.SubscribeNetSerializable<CharExitSnapshot>(HandleCharExitSnapshot);
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
                //Console.WriteLine($"[STATUS] Posição Autoritativa: ({currentPosFromServer.X},{currentPosFromServer.Y}) | Posição Predita: ({predictedPos.X},{predictedPos.Y})");
                
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
                    
                    Console.WriteLine($"[PREDICTION] Nova posição predi ta: {predictedPos.X},{predictedPos.Y}");
                }
            }
            Thread.Sleep(10);
        }
        
        _client.Stop();
        Console.WriteLine("Cliente finalizado.");
    }
    
    // Em Simulation.Client/Program.cs

    static void HandleGameSnapshot(GameSnapshot snapshot)
    {
        Console.WriteLine($"[GAME STATE] Bem-vindo ao mapa {snapshot.MapId}. Você é o CharId: {snapshot.CharId}");
        _state = ClientState.InGame;

        OtherPlayers.Clear(); // Limpa a lista antiga
        foreach (var charSnap in snapshot.AllEntities)
        {
            if (charSnap.CharId.CharacterId != CharId) // Se não for o nosso próprio personagem
            {
                OtherPlayers[charSnap.CharId.CharacterId] = charSnap.Position.Position;
                Console.WriteLine($" -> Jogador {charSnap.Info.Name} (ID:{charSnap.CharId.CharacterId}) está em ({charSnap.Position.Position.X},{charSnap.Position.Position.Y})");
            }
            else // Se for o nosso, atualiza nossa posição autoritativa
            {
                currentPosFromServer = charSnap.Position.Position;
                predictedPos = currentPosFromServer;
                Console.WriteLine($" -> Sua posição inicial é: ({currentPosFromServer.X},{currentPosFromServer.Y})");
            }
        }
    }
    
    // Em Simulation.Client/Program.cs
    static void HandleCharSnapshot(CharSnapshot snapshot)
    {
        if (snapshot.CharId.CharacterId == CharId) return; // Ignora nosso próprio snapshot aqui

        OtherPlayers[snapshot.CharId.CharacterId] = snapshot.Position.Position;
        Console.WriteLine($"[ENTROU] Jogador {snapshot.Info.Name} (ID:{snapshot.CharId.CharacterId}) entrou no mapa em ({snapshot.Position.Position.X},{snapshot.Position.Position.Y})");
    }
    
    // Em Simulation.Client/Program.cs
    static void HandleCharExitSnapshot(CharExitSnapshot snapshot)
    {
        if (OtherPlayers.Remove(snapshot.CharId))
        {
            Console.WriteLine($"[SAIU] Jogador com CharId {snapshot.CharId} desconectou-se.");
        }
    }

    // Em Simulation.Client/Program.cs
    static void HandleMoveSnapshot(MoveSnapshot snapshot)
    {
        // Se o snapshot é para o nosso personagem
        if (snapshot.CharId == CharId) 
        {
            currentPosFromServer = snapshot.NewPosition;
            if (predictedPos != currentPosFromServer)
            {
                Console.WriteLine($"[RECONCILIATION] Posição corrigida do servidor: ({currentPosFromServer.X},{currentPosFromServer.Y})");
                predictedPos = currentPosFromServer;
            }
            else
            {
                Console.WriteLine($"[SNAPSHOT] Posição confirmada pelo servidor: ({currentPosFromServer.X},{currentPosFromServer.Y})");
            }
        }
        // Se o snapshot é para outro jogador
        else if (OtherPlayers.ContainsKey(snapshot.CharId))
        {
            OtherPlayers[snapshot.CharId] = snapshot.NewPosition;
            Console.WriteLine($"[MOVIMENTO] Jogador {snapshot.CharId} moveu-se para ({snapshot.NewPosition.X},{snapshot.NewPosition.Y})");
        }
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