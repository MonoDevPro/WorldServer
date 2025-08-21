using Arch.Core;
using Simulation.Core.Commons;
using Simulation.Core.Components;

namespace Simulation.Core.Abstractions.In;

/// <summary>
/// Interface para enfileirar comandos que serão aplicados pelos sistemas de simulação.
/// Thread-safe para uso pela camada de rede.
/// </summary>
public interface ISimulationRequests
{
    void EnqueueMove(Requests.Move request);
    void EnqueueTeleport(Requests.Teleport request);
    void EnqueueAttack(Requests.Attack request);

    // Métodos de drenagem para o loop da simulação consumir todos os comandos pendentes a cada tick
    bool TryDequeueMove(out Requests.Move request);
    bool TryDequeueTeleport(out Requests.Teleport request);
    bool TryDequeueAttack(out Requests.Attack request);
}