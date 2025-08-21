using Arch.Core;
using Arch.Core.Utils;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Components;

namespace Simulation.Core.Systems;

public sealed partial class SpawnDespawnSystem(World world) : BaseSystem<World, float>(world)
{
    [Query]
    [All<Lifetime>]
    private void ProcessLifetime([Data]in float dt, Entity e, ref Lifetime life)
    {
        // Despawn by Lifetime
            life.RemainingSeconds -= dt;
            if (life.RemainingSeconds <= 0)
                World.Destroy(e);
    }
}
