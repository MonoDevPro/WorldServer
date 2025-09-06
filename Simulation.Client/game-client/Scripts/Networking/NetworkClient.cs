using System.Collections.Concurrent;
using Godot;
using LiteNetLib;
using LiteNetLib.Utils;

namespace GameClient.Scripts.Networking;

public sealed class NetworkClient : INetEventListener
{
    private NetManager? _client;
    private NetPeer? _serverPeer;
    private readonly ConcurrentQueue<object> _snapshotQueue = new();
    private readonly NetDataWriter _writer = new();

    public bool IsConnected => _serverPeer?.ConnectionState == ConnectionState.Connected;
    public event System.Action? Connected;
    public IEnumerable<object> DequeueSnapshots()
    {
        while (_snapshotQueue.TryDequeue(out var obj)) yield return obj;
    }

    public void Connect(string host, int port, string key)
    {
        if (_client != null) return;
        _client = new NetManager(this) { UnsyncedEvents = true, UnconnectedMessagesEnabled = false, IPv6Enabled = false };
        _client.Start();
        _client.Connect(host, port, key);
    }

    public void Disconnect()
    {
        _serverPeer?.Disconnect();
        _client?.Stop();
        _client = null;
        _serverPeer = null;
    }

    public void PollEvents() => _client?.PollEvents();

    // Intent send wrappers
    public void Send(System.Action<NetDataWriter> write)
    {
        if (_serverPeer == null) return;
        _writer.Reset();
        write(_writer);
        _serverPeer.Send(_writer, DeliveryMethod.ReliableOrdered);
    }

    // INetEventListener impl
    public void OnConnectionRequest(ConnectionRequest request) => request.AcceptIfKey("worldserver-key-from-json");  
    public void OnPeerConnected(NetPeer peer)
    {
        _serverPeer = peer;
        GD.Print("Connected to server");
    Connected?.Invoke();
    }
    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        GD.Print($"Disconnected: {disconnectInfo.Reason}");
        _serverPeer = null;
    }
    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        => GD.PrintErr($"Network error: {socketError}");
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        var snapshot = PacketProcessor.ReadSnapshot(reader);
        if (snapshot != null) _snapshotQueue.Enqueue(snapshot);
        reader.Recycle();
    }
}
