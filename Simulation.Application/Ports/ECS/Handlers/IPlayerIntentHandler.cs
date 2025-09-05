using Simulation.Application.DTOs;
using Simulation.Application.DTOs.Intents;

namespace Simulation.Application.Ports.ECS.Handlers;

public interface IPlayerIntentHandler : IDisposable
{
    // Server-authoritative: client doesn't send state here.
    void HandleIntent(in EnterIntent intent);
    void HandleIntent(in ExitIntent intent);
    void HandleIntent(in MoveIntent intent);
    void HandleIntent(in TeleportIntent intent);
    void HandleIntent(in AttackIntent intent);
}