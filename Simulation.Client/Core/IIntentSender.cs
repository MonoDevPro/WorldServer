using Simulation.Core.Abstractions.Adapters;

namespace Simulation.Client.Core;

/// <summary>
/// Interface para envio de intents do cliente para o servidor.
/// Abstração que permite enviar comandos do jogador.
/// </summary>
public interface IIntentSender
{
    void SendIntent(in EnterIntent intent);
    void SendIntent(in ExitIntent intent);
    void SendIntent(in MoveIntent intent);
    void SendIntent(in AttackIntent intent);
    void SendIntent(in TeleportIntent intent);
}