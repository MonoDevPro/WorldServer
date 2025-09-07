using Simulation.Application.Ports.Network.Domain.Enums;
using Simulation.Application.Ports.Network.Domain.Models;

namespace Simulation.Application.Ports.Network.Outbound;

/// <summary>
/// Interface para o serviço de envio de pacotes
/// </summary>
public interface IPacketSender
{
    bool SendPacket<T>(int peerId, T packet, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered)
        where T : IPacket;
    bool Broadcast<T>(T packet, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered)
        where T : IPacket;
}