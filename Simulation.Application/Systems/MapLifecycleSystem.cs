using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Simulation.Application.DTOs;

namespace Simulation.Application.Systems;

public sealed partial class MapLifecycleSystem(
    World world,
    ILogger<MapLifecycleSystem> logger
    ) : BaseSystem<World, float>(world)
{
    [Query]
    [All<LoadMapIntent>]
    private void OnLoadMap(in Entity e, in LoadMapIntent intent)
    {
        EventBus.Send(new LoadMapSnapshot(intent.MapId));
        World.Remove<LoadMapIntent>(e);
    }

    [Query]
    [All<UnloadMapIntent>]
    private void OnUnloadMap(in Entity e, in UnloadMapIntent intent)
    {
        EventBus.Send(new UnloadMapSnapshot(intent.MapId));
        World.Destroy(e);
    }
}