using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Adapters.Char;

namespace Simulation.Core.Abstractions.Ports;

public interface IIntentHandler
{
    void HandleIntent(in EnterIntent intent);
    void HandleIntent(in ExitIntent intent);
    void HandleIntent(in MoveIntent intent);
    void HandleIntent(in TeleportIntent intent);
    void HandleIntent(in AttackIntent intent);
}