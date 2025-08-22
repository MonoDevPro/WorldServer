using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Components;
using Simulation.Core.Utilities;

namespace Simulation.Core.Systems;

public sealed partial class TeleportSystem(World world, BlockingIndex blocking, BoundsIndex bounds) 
    : BaseSystem<World, float>(world: world)
{
    [Query]
    [All<TeleportIntent>]
    [All<MapRef>]
    [All<TilePosition>]
    private void Process(in Entity entity, in TeleportIntent cmd, in MapRef mapRef, ref TilePosition tilePos)
    {
        // Bounds check
        var target = new TilePosition { Position = cmd.Target };
        bounds.RebuildIfDirty(World);
        if (bounds.TryGet(mapRef.MapId, out var bounds1) && !bounds1.Contains(target)) return;

        // Blocking check
        blocking.RebuildIfDirty(World);
        if (blocking.IsBlocked(mapRef.MapId, target.Position)) return;

        tilePos.Position = cmd.Target;
        
        // Clear intent after processing
        World.Remove<TeleportIntent>(entity);
    }
}
