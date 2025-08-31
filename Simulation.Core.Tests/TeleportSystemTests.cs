using System;
using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Simulation.Core;
using Simulation.Core.Abstractions.Adapters;
using Simulation.Core.Abstractions.Adapters.Char;
using Simulation.Core.Abstractions.Adapters.Map;
using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Abstractions.Ports;
using Simulation.Core.Abstractions.Ports.Char;
using Simulation.Core.Abstractions.Ports.Index;
using Simulation.Core.Abstractions.Ports.Map;
using Simulation.Core.Abstractions.Adapters.Spatial;
using Simulation.Core.Adapters;
using Simulation.Core.Systems;
using Xunit;

namespace Simulation.Core.Tests;

public class TeleportSystemTests
{
    private static (IServiceProvider sp, World world, SimulationRunner runner, TestSnapshotPublisher pub) CreateSim()
    {
        var services = new ServiceCollection();
    services.AddOptions();
    services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.Configure<WorldOptions>(opts => { });
        services.AddSingleton(WorldFactory.Create());
        services.AddSingleton<IMapIndex, MapIndex>();
    services.AddSingleton<ISpatialIndex, QuadTreeIndex>();
    services.AddSingleton<ICharIndex, CharIndex>();
    services.AddSingleton<ICharTemplateRepository, InMemoryCharTemplateRepository>();
        services.AddSingleton<IntentsHandlerSystem>();
        services.AddSingleton<IIntentHandler>(sp => sp.GetRequiredService<IntentsHandlerSystem>());
        services.AddSingleton<MapLoaderSystem>();
        services.AddSingleton<IMapLoaderSystem>(sp => sp.GetRequiredService<MapLoaderSystem>());
        var pub = new TestSnapshotPublisher();
        services.AddSingleton<ISnapshotPublisher>(pub);
        services.AddSingleton<SnapshotPublisherSystem>();
        services.AddSingleton<PlayerLifecycleSystem>();
        services.AddSingleton<LifetimeSystem>();
        services.AddSingleton<GridMovementSystem>();
        services.AddSingleton<TeleportSystem>();
        services.AddSingleton<AttackSystem>();
        services.AddSingleton<SpatialIndexSyncSystem>();
        services.AddSingleton<SimulationPipeline>();
        services.AddSingleton<SimulationRunner>();

    var sp = services.BuildServiceProvider();
        return (sp, sp.GetRequiredService<World>(), sp.GetRequiredService<SimulationRunner>(), pub);
    }

    private static void Step(SimulationRunner runner, int times = 1)
    {
        for (int i = 0; i < times; i++) runner.Update(1f/30f);
    }

    [Fact]
    public void Teleport_publishes_new_mapid()
    {
        var (sp, world, runner, pub) = CreateSim();
    var mapLoader = sp.GetRequiredService<IMapLoaderSystem>();
    // register two simple 10x10 maps
    var mt0 = new MapTemplate { MapId = 0, Name = "m0", Width = 10, Height = 10, TilesRowMajor = new TileType[100], CollisionRowMajor = new byte[100] };
    var mt1 = new MapTemplate { MapId = 1, Name = "m1", Width = 10, Height = 10, TilesRowMajor = new TileType[100], CollisionRowMajor = new byte[100] };
    mapLoader.EnqueueMapData(MapData.CreateFromTemplate(mt0));
    mapLoader.EnqueueMapData(MapData.CreateFromTemplate(mt1));
        Step(runner, 1);

    var intents = sp.GetRequiredService<IIntentHandler>();
    intents.HandleIntent(new EnterIntent(123));
        Step(runner, 1); // process enter

        // teleport to map 1
    intents.HandleIntent(new TeleportIntent(123, 1, new Position { X = 2, Y = 2 }));
        Step(runner, 1);

        Assert.Contains(pub.Teleports, t => t.CharId == 123 && t.MapId == 1 && t.Position.X == 2 && t.Position.Y == 2);
    }

    [Fact]
    public void Enter_and_exit_produce_snapshots()
    {
        var (sp, world, runner, pub) = CreateSim();
    var mapLoader = sp.GetRequiredService<IMapLoaderSystem>();
    var mt0 = new MapTemplate { MapId = 0, Name = "m0", Width = 10, Height = 10, TilesRowMajor = new TileType[100], CollisionRowMajor = new byte[100] };
    mapLoader.EnqueueMapData(MapData.CreateFromTemplate(mt0));
        Step(runner, 1);

    var intents = sp.GetRequiredService<IIntentHandler>();
    intents.HandleIntent(new EnterIntent(555));
        Step(runner, 1);
    Assert.Contains(pub.Enters, e => e.charId == 555);
        Assert.Contains(pub.Chars, c => c.CharId == 555 && c.MapId == 0);

    intents.HandleIntent(new ExitIntent(555));
        Step(runner, 1);
        Assert.Contains(pub.Exits, e => e.CharId == 555);
    }
}
