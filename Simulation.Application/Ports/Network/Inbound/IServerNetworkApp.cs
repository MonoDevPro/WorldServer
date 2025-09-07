using Simulation.Application.Options;
using Simulation.Application.Ports.Network.Outbound;

namespace Simulation.Application.Ports.Network.Inbound;

public interface IServerNetworkApp
{
    NetworkOptions Options { get; }
    IConnectionManager ConnectionManager { get; }
    IPacketSender PacketSender { get; }
    IPacketRegistry PacketRegistry { get; }
    INetworkEventBus EventBus { get; }
    bool Start();
    void Stop();
    void DisconnectPeer(int peerId);
    void Update(float deltaTime);
    void Dispose();
}