using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Core.Systems;

public sealed partial class TeleportSystem(World world, IMapIndex mapIndex, ISpatialIndex grid, IEntityIndex entityIndex) 
    : BaseSystem<World, float>(world: world)
{
    [Query]
    [All<TeleportIntent>]
    [All<MapId>]
    [All<Position>]
    private void Process(in Entity entity, in TeleportIntent cmd, in MapId mapId, ref Position pos)
    {
        if (!mapIndex.TryGetMap(mapId.Value, out var map))
        {
            // mapa não encontrado — abortar
            World.Remove<TeleportIntent>(entity);
            return;
        }
        
        bool blocked = false;
        var targetPos = cmd.Target;

        grid.QueryAABB(mapId.Value, targetPos.X, targetPos.Y, targetPos.X, targetPos.Y, eid =>
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
        var oldPos = pos.Value;
        World.Add<SpatialDirty>(entity, new SpatialDirty{New = targetPos, Old = oldPos});
        grid.EnqueueUpdate(entity.Id, mapId.Value, oldPos, targetPos);

        // Move a entidade
        pos.Value = targetPos;

        // Clear intent after processing
        World.Remove<TeleportIntent>(entity);
    }
}