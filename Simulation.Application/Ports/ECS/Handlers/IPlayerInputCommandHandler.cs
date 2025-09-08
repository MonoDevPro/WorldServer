
using Simulation.Domain;

namespace Simulation.Application.Ports.ECS.Handlers;

public interface IPlayerInputCommandHandler : IDisposable
{
    // Server-authoritative: client doesn't send state here.
    void HandleIntent(int charId, in MoveIntent intent);
    void HandleIntent(int charId, in TeleportIntent intent);
    void HandleIntent(int charId, in AttackIntent intent);
}