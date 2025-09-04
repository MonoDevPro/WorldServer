using Simulation.Application.DTOs;
using Simulation.Domain.Templates;

namespace Simulation.Application.Ports.Char;

public interface ICharIntentHandler : IDisposable
{
    void HandleIntent(in EnterIntent intent, CharTemplate template);
    void HandleIntent(in ExitIntent intent);
    void HandleIntent(in MoveIntent intent);
    void HandleIntent(in TeleportIntent intent);
    void HandleIntent(in AttackIntent intent);
}