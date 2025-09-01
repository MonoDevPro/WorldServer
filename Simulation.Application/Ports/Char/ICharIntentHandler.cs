using Simulation.Application.DTOs;

namespace Simulation.Application.Ports.Char;

public interface ICharIntentHandler : IDisposable
{
    void HandleIntent(in EnterIntent intent);
    void HandleIntent(in ExitIntent intent);
    void HandleIntent(in MoveIntent intent);
    void HandleIntent(in TeleportIntent intent);
    void HandleIntent(in AttackIntent intent);
}