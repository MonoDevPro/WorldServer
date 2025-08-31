using Simulation.Application.DTOs;

namespace Simulation.Client.Core;

/// <summary>
/// Interface para o manipulador de snapshots do cliente.
/// Recebe snapshots do servidor e atualiza o estado local do ECS.
/// </summary>
public interface ISnapshotHandler
{
    void HandleSnapshot(in EnterSnapshot snapshot);
    void HandleSnapshot(in CharSnapshot snapshot);
    void HandleSnapshot(in ExitSnapshot snapshot);
    void HandleSnapshot(in MoveSnapshot snapshot);
    void HandleSnapshot(in AttackSnapshot snapshot);
    void HandleSnapshot(in TeleportSnapshot snapshot);
}