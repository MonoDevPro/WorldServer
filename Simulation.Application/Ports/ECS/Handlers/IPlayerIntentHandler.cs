using Simulation.Application.DTOs;
using Simulation.Application.DTOs.Intents;

namespace Simulation.Application.Ports.ECS.Handlers;

public interface IPlayerIntentHandler : IDisposable
{
    void HandleIntent(in EnterIntent intent, PlayerStateDto state);
    void HandleIntent(in ExitIntent intent);
    void HandleIntent(in MoveIntent intent);
    void HandleIntent(in TeleportIntent intent);
    void HandleIntent(in AttackIntent intent);
}