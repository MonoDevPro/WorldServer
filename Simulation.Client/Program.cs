using LiteNetLib;
using LiteNetLib.Utils;

namespace Simulation.Client;

enum Step { ConnectWait, Teleport, Move, Done }

class Program
{
    static void Main()
    {
        var evt = new EventBasedNetListener();
        var client = new NetManager(evt) { IPv6Enabled = false };
        client.Start();
        var peer = client.Connect("127.0.0.1", 27015, "worldserver-key");

        Console.WriteLine("Client connecting...");

        // event logs
        evt.PeerConnectedEvent += p => Console.WriteLine($"Connected: {p.EndPoint}");
        evt.PeerDisconnectedEvent += (p, info) => Console.WriteLine($"Disconnected: {info.Reason}");
        evt.NetworkErrorEvent += (endPoint, error) => Console.WriteLine($"Error: {error}");

        var step = Step.ConnectWait;
        var last = DateTime.UtcNow;

        while (!Console.KeyAvailable)
        {
            client.PollEvents();

            switch (step)
            {
                case Step.ConnectWait:
                    if (peer != null && peer.ConnectionState == ConnectionState.Connected)
                    {
                        Console.WriteLine("Sending teleport to (5,5) on map 1 for char 1");
                        SendTeleport(peer, charId: 1, mapId: 1, x: 5, y: 5);
                        step = Step.Move;
                        last = DateTime.UtcNow;
                    }
                    break;
                case Step.Move:
                    if ((DateTime.UtcNow - last).TotalMilliseconds > 200 && peer is not null)
                    {
                        // Envia alguns passos para leste
                        SendMove(peer, 1, 1, 1, 0);
                        last = DateTime.UtcNow;
                    }
                    break;
            }

            Thread.Sleep(10);
        }

        client.Stop();
    }

    static void SendMove(NetPeer peer, int charId, int mapId, int dirX, int dirY)
    {
        var writer = new NetDataWriter();
        writer.Put((byte)1);
        writer.Put(charId);
        writer.Put(mapId);
        writer.Put(dirX);
        writer.Put(dirY);
        peer.Send(writer, DeliveryMethod.Unreliable);
    }

    static void SendTeleport(NetPeer peer, int charId, int mapId, int x, int y)
    {
        var writer = new NetDataWriter();
        writer.Put((byte)2);
        writer.Put(charId);
        writer.Put(mapId);
        writer.Put(x);
        writer.Put(y);
        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }
}