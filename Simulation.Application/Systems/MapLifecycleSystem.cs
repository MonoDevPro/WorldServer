using System.Runtime.InteropServices;
using Arch.Bus;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Commons.Pools;
using Simulation.Domain.Components;
using Arch.System.SourceGenerator;
using Simulation.Application.Ports.Char;
using Simulation.Application.Ports.Map;
using Simulation.Application.Ports.Map.Indexers;
using Simulation.Domain.Templates;

namespace Simulation.Application.Systems;

public sealed partial class MapLifecycleSystem(
    World world,
    IMapIndex mapIndex,
    ILogger<CharLifecycleSystem> logger)
    : BaseSystem<World, float>(world: world)
{
    [Query]
    [All<LoadMapIntent>]
    private void OnLoadMap(in Entity entity, in LoadMapIntent intent)
    {
        mapIndex.Register(intent.MapId, entity);
        
        EventBus.Send(new LoadMapSnapshot(intent.MapId));
        World.Remove<LoadMapIntent>(entity);
    }

    [Query]
    [All<UnloadMapIntent>]
    private void OnDespawnRequest(in Entity e, in ExitIntent intent, in CharId cid, in MapId mid)
    {
        if (mapIndex.TryGet(mid.Value, out var mapEntity))
            mapIndex.Unregister(mid.Value);
        
        EventBus.Send(new UnloadMapSnapshot(mid.Value));
        World.Destroy(e);
    }

}
