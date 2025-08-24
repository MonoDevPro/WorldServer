using System;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Abstractions.Commons.Components;
using Simulation.Core.Abstractions.In;
using Simulation.Core.Abstractions.Intents.In;
using Simulation.Core.Abstractions.Out;
using Simulation.Core.Utilities;

namespace Simulation.Core.Systems;

public sealed partial class TeleportSystem(World world, ISpatialIndex grid, IEntityIndex entityIndex) 
    : BaseSystem<World, float>(world: world)
{
    [Query]
    [All<TeleportIntent>]
    [All<MapRef>]
    [All<TilePosition>]
    private void Process(in Entity entity, in TeleportIntent cmd, in MapRef mapRef, ref TilePosition tilePos)
    {
        if (!MapIndex.TryGetMap(mapRef.MapId, out var map))
        {
            // mapa não encontrado — abortar
            World.Remove<TeleportIntent>(entity);
            return;
        }
        
        bool blocked = false;
        var targetPos = cmd.Target;

        grid.QueryAABB(mapRef.MapId, targetPos.X, targetPos.Y, targetPos.X, targetPos.Y, eid =>
        {
            var entity = entityIndex.TryGetByEntityId(eid, out var en) ? en : default;
            if (entity == default) return;
            
            if (World.Has<Blocking>(entity))
            {
                blocked = true;
            }
        });

        if (blocked)
        {
            World.Remove<TeleportIntent>(entity);
            return;
        }

        // Atualiza índice espacial: caso a entidade já esteja registrada, faz UpdatePosition; senão Registra
        var oldPos = tilePos.Position;
        try
        {
            ref var dirty = ref World.AddOrGet<SpatialIndexDirty>(entity);
            dirty.Old = oldPos;
            dirty.New = targetPos;
            dirty.MapId = mapRef.MapId;
            grid.EnqueueUpdate(entity.Id, mapRef.MapId, oldPos, targetPos);
        }
        catch
        {
            // fallback seguro: unregister + register
            try { grid.Unregister(entity.Id, mapRef.MapId); } catch { }
            grid.Register(entity.Id, mapRef.MapId, targetPos);
        }

        // Move a entidade
        tilePos.Position = targetPos;

        // Clear intent after processing
        World.Remove<TeleportIntent>(entity);
    }
}
