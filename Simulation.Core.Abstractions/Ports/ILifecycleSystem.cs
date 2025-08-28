using Simulation.Core.Abstractions.Adapters.Data;

namespace Simulation.Core.Abstractions.Ports;

public interface ILifecycleSystem
{
    void EnqueueSpawn(CharTemplate template);
    void EnqueueDespawnByCharId(int charId);
}