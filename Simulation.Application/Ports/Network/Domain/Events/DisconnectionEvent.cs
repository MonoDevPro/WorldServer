using Simulation.Application.Ports.Network.Domain.Enums;

namespace Simulation.Application.Ports.Network.Domain.Events;

/// <summary>
/// Evento disparado quando uma desconexão acontece
/// </summary>
public class DisconnectionEvent(int peerId, DisconnectReason reason)
{
    // Evento de domínio para notificar quando uma desconexão ocorre.
    // Permite que handlers de persistência, métricas ou lógica de negócio reajam à saída de um peer.
    // Mantém o domínio desacoplado da infraestrutura de rede.
    public int PeerId { get; } = peerId;
    public DisconnectReason Reason { get; } = reason;
}