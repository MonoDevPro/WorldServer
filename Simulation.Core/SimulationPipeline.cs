using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.Systems;

namespace Simulation.Core;

public class SimulationPipeline : List<BaseSystem<World, float>>
{
    public SimulationPipeline(IServiceProvider provider)
    {
        Add(provider.GetRequiredService<IndexUpdateSystem>());
        Add(provider.GetRequiredService<SpawnDespawnSystem>());
        Add(provider.GetRequiredService<GridMovementSystem>());
        Add(provider.GetRequiredService<TeleportSystem>());
        Add(provider.GetRequiredService<AttackSystem>());
        Add(provider.GetRequiredService<IntentsDequeueSystem>());
        Add(provider.GetRequiredService<SnapshotPostSystem>());
    }
}