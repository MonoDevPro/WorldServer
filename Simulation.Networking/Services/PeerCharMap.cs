using Simulation.Application.Ports.Network;
using Simulation.Application.Ports.Network.Domain.Events;
using Simulation.Application.Ports.Network.Inbound;
using Simulation.Application.Services.Commons;

namespace Simulation.Networking.Services;

public sealed class PeerCharMap : DefaultIndex<int, int>, IPlayerConnectionMap
{
    private readonly INetworkEventBus _eventBus;
    
    public PeerCharMap(INetworkEventBus eventBus)
    {
        _eventBus = eventBus;
        // Se inscreve nos eventos de desconexão para limpar o mapa automaticamente.
        _eventBus.Subscribe<DisconnectionEvent>(OnPeerDisconnected);
    }
    
    private void OnPeerDisconnected(DisconnectionEvent e)
    {
        Unregister(e.PeerId);
    }
}