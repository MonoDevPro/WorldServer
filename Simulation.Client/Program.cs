using LiteNetLib;
using LiteNetLib.Utils;
using System.Diagnostics;

namespace Simulation.Client;

// Adicionamos um enum para controlar o estado do cliente
enum ClientState { Connecting, Connected, InGame }

class Program
{
    private const int CharId = 1; 
    private static volatile ClientState _state = ClientState.Connecting; // Estado inicial

    static void Main()
    {
        var listener = new EventBasedNetListener();
        var client = new NetManager(listener) { UnsyncedEvents = false, IPv6Enabled = false };
        client.Start();
        var peer = client.Connect("127.0.0.1", 27015, "worldserver-key");

        Console.WriteLine("Cliente de Testes Iniciado.");
        Console.WriteLine("Tentando conectar ao servidor...");
        
        listener.PeerConnectedEvent += p =>
        {
            Console.WriteLine($"[CONECTADO] ao servidor: {p.EndPoint}");
            Console.WriteLine("Enviando comando para entrar no jogo...");
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
            HandleServerMessage(dataReader);
            dataReader.Recycle();
        };

        var stopwatch = Stopwatch.StartNew();
        var lastMoveTime = stopwatch.Elapsed;

        Console.WriteLine("\n--- Controles ---");
        Console.WriteLine("A - Atacar");
        Console.WriteLine("Q - Sair");
        Console.WriteLine("-----------------\n");
        
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
                    if (key == ConsoleKey.A)
                    {
                        Console.WriteLine("[AÇÃO] Enviando ataque...");
                        SendAttack(peer, CharId);
                    }
                }

                if ((stopwatch.Elapsed - lastMoveTime).TotalMilliseconds > 200)
                {
                    SendMove(peer, CharId, 1, 0); // Move para a direita
                    lastMoveTime = stopwatch.Elapsed;
                }
            }

            Thread.Sleep(15);
        }

        client.Stop();
        Console.WriteLine("Cliente finalizado.");
    }
    
    // As funções HandleServerMessage, SendEnterGame, SendMove e SendAttack permanecem as mesmas.
    static void HandleServerMessage(NetPacketReader reader)
    {
        try
        {
            var msgType = reader.GetByte();
            switch (msgType)
            {
                case 1: // MoveSnapshot
                {
                    var charId = reader.GetInt();
                    var posX = reader.GetInt();
                    var posY = reader.GetInt();
                    var dirX = reader.GetInt();
                    var dirY = reader.GetInt();
                    Console.WriteLine($"[SNAPSHOT] Move -> Char:{charId} | Pos:({posX},{posY}) | Dir:({dirX},{dirY})");
                    break;
                }
                case 2: // AttackSnapshot
                {
                    var charId = reader.GetInt();
                    Console.WriteLine($"[SNAPSHOT] Attack -> Char:{charId} iniciou um ataque!");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao ler snapshot do servidor: {ex.Message}");
        }
    }
    
    static void SendEnterGame(NetPeer peer, int charId)
    {
        var writer = new NetDataWriter();
        writer.Put((byte)0);
        writer.Put(charId);
        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }
    
    static void SendMove(NetPeer peer, int charId, int dirX, int dirY)
    {
        var writer = new NetDataWriter();
        writer.Put((byte)1);
        writer.Put(charId);
        writer.Put(dirX);
        writer.Put(dirY);
        peer.Send(writer, DeliveryMethod.Unreliable);
    }

    static void SendAttack(NetPeer peer, int charId)
    {
        var writer = new NetDataWriter();
        writer.Put((byte)2);
        writer.Put(charId);
        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }
}