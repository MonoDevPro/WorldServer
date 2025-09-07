using Simulation.Application.Options;
using Simulation.Application.Ports.Network.Domain.Models;
using Simulation.Application.Ports.Network.Outbound;

namespace Simulation.Application.Ports.Network.Inbound;

public interface IClientNetworkApp : IDisposable
{
    NetworkOptions Options { get; }
    IConnectionManager ConnectionManager { get; }
    IPacketSender PacketSender { get; }
    IPacketRegistry PacketRegistry { get; }
    INetworkEventBus EventBus { get; }
    Task<ConnectionResult> ConnectAsync();
    bool TryConnect(out ConnectionResult result);
    void Disconnect();
    void Update(float deltaTime);
}