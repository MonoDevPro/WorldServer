using Simulation.Application.DTOs;
using Simulation.Networking.DTOs.Intents;

namespace Simulation.Client.Core;

/// <summary>
/// Interface para envio de intents do cliente para o servidor.
/// Abstração que permite enviar comandos do jogador.
/// </summary>
public interface IIntentSender
{
    void SendIntent(in EnterIntentPacket intent);
    void SendIntent(in ExitIntentPacket intent);
    void SendIntent(in MoveIntentPacket intent);
    void SendIntent(in AttackIntentPacket intent);
    void SendIntent(in TeleportIntentPacket intent);
}