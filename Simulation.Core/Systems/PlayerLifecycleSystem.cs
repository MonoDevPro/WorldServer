using System;
using System.Collections.Concurrent;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Adapters.Char;
using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Core.Adapters;

public sealed partial class PlayerLifecycleSystem(
    World world,
    IEntityIndex entityIndex,
    ISpatialIndex spatialIndex,
    ILogger<PlayerLifecycleSystem> logger)
    : BaseSystem<World, float>(world)
{
    
    [Query]
    [All<EnterGameIntent>]
    [All<CharRuntimeTemplate>]
    private void OnSpawnRequest(in Entity intentEntity, in CharRuntimeTemplate runtime)
    {
        var charEntity = CharFactory.CreateEntityFromRuntimeTemplate(World, runtime);
        var charId = runtime.CharId.Value;
        
        entityIndex.Register(charId, charEntity);
        spatialIndex.Register(charId, runtime.MapId.Value, runtime.Position.Value);
        World.Remove<CharRuntimeTemplate>(intentEntity);
    }

    [Query]
    [All<ExitGameIntent>]
    [None<CharRuntimeTemplate>]
    private void OnDespawnRequest(in Entity intentEntity, in ExitGameIntent intent)
    {
        if (!entityIndex.TryGetByCharId(intent.CharId, out var charEntity))
        {
            logger.LogWarning("DespawnRequest: CharId {CharId} not found, cannot despawn.", intent.CharId);
            World.Destroy(intentEntity);
            return;
        }
        
        // remove do spatial index
        if (World.Has<MapId>(charEntity))
        {
            var mapId = World.Get<MapId>(charEntity).Value;
            spatialIndex.Unregister(charEntity.Id, mapId);
        }

        // remove do entity index
        entityIndex.UnregisterByCharId(intent.CharId);
        
        // Obter o runtimeTemplate antes de destruir a entidade
        World.Add<CharRuntimeTemplate>(intentEntity, CharFactory.CreateRuntimeTemplate(World, charEntity));

        // remove a entidade do mundo
        World.Destroy(charEntity);

        logger.LogInformation("Despawned CharId {CharId} (Entity {EntityId})", intent.CharId, charEntity.Id);
    }
}