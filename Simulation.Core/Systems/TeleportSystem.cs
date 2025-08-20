using Arch.Core;
using Simulation.Core.Components;
using Simulation.Core.Utilities;

namespace Simulation.Core.Systems;

public sealed class TeleportSystem(World world, BlockingIndex blocking, BoundsIndex bounds)
{
    public readonly record struct Teleport(Entity Entity, int MapId, TilePosition Target);

    public bool Apply(in Teleport cmd)
    {
        var e = cmd.Entity;
        if (!world.IsAlive(e)) return false;

        // Bounds check
        var mapId = cmd.MapId;
        var target = cmd.Target;
        bounds.RebuildIfDirty(world);
        if (bounds.TryGet(mapId, out var bounds1) && !bounds1.Contains(target)) return false;

        // Blocking check
        blocking.RebuildIfDirty(world);
        if (blocking.IsBlocked(mapId, target.Position)) return false;

        // Apply teleport (set map and tile)
        ref var mapRef = ref world.AddOrGet<MapRef>(e);
        mapRef.MapId = mapId;

        ref var tilePos = ref world.AddOrGet<TilePosition>(e);
        tilePos.Position = target.Position;

        return true;
    }
}
