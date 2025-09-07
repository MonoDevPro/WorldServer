using Simulation.Application.Ports.Network.Domain.Models;

namespace Simulation.Application.Ports.Network.Outbound;

/// <summary>
/// Interface para o serviço de rede do cliente
/// </summary>
public interface IClientNetworkService
{
    bool TryConnect(string serverAddress, int port, out ConnectionResult result);
    void Disconnect();
    void Update();
}