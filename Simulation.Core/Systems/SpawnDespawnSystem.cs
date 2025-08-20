using Arch.Core;
using Arch.Core.Utils;
using Arch.System;
using Simulation.Core.Components;

namespace Simulation.Core.Systems;

public sealed class SpawnDespawnSystem : ISystem<float>
{
    public World World { get; }
    public SpawnDespawnSystem(World world) => World = world;

    public void Initialize() { }
    public void BeforeUpdate(in float t) { }
    public void Update(in float t)
    {
        var dt = t;
        // Despawn by Lifetime
        var query = new QueryDescription().WithAll<Lifetime>();
        var toDestroy = new List<Entity>();
        World.Query(in query, (ref Entity entity, ref Lifetime life) =>
        {
            life.RemainingSeconds -= dt;
            if (life.RemainingSeconds <= 0)
                toDestroy.Add(entity);
        });
        foreach (var e in toDestroy)
            World.Destroy(e);
    }
    public void AfterUpdate(in float t) { }
    public void Dispose() { }
}
