/*
using Arch.Core;
using BenchmarkDotNet.Attributes;
using Simulation.Core.Components;
using Simulation.Core.Systems;
using Simulation.Core.Utilities;

namespace Simulation.Core.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class TeleportBenchmarks
{
    [Params(1_000, 5_000, 10_000)]
    public int Entities;

    private World _world = null!;
    private TeleportSystem _teleport = null!;
    private BlockingIndex _blocking = null!;
    private BoundsIndex _bounds = null!;
    private TeleportSystem.Teleport[] _commands = null!;
    private int _cmdIndex;

    [GlobalSetup]
    public void Setup()
    {
        _world = World.Create(Entities + 5000);
        _blocking = new BlockingIndex();
        _bounds = new BoundsIndex();
        // Two maps with bounds
        _world.Create(new Bounds{ MinX=-500, MinY=-500, MaxX=500, MaxY=500}, new MapRef{ MapId = 1 });
        _world.Create(new Bounds{ MinX=-300, MinY=-300, MaxX=300, MaxY=300}, new MapRef{ MapId = 2 });
        _bounds.RebuildIfDirty(_world);
        var rnd = new Random(123);
        // Some blocking tiles (they should NOT receive teleport commands)
        var blockingCount = Entities / 50;
        for (int i=0;i<blockingCount;i++)
        {
            _world.Create(new Blocking(), new TilePosition{ X = rnd.Next(-50,51), Y = rnd.Next(-50,51)}, new MapRef{ MapId = 1 });
        }
        _blocking.RebuildIfDirty(_world);
        for (int i = 0; i < Entities; i++)
        {
            _world.Create(new TilePosition{ X=0, Y=0}, new MapRef{ MapId = 1 });
        }
        _teleport = new TeleportSystem(_world, _blocking, _bounds);
        // Pre-generate teleport commands only for non-blocking entities
        var list = new List<TeleportSystem.Teleport>(Entities);
        int idx = 0;
        var query = new QueryDescription().WithAll<TilePosition, MapRef>();
        _world.Query(in query, (ref Entity e, ref TilePosition _, ref MapRef m) =>
        {
            if (_world.Has<Blocking>(e)) return; // skip blocking tiles
            var mapId = (idx & 1) == 0 ? 1 : 2;
            list.Add(new TeleportSystem.Teleport(e, mapId, new TilePosition
            {
                X = (idx % 200) - 100,
                Y = ((idx*7)%200) - 100
            }));
            idx++;
        });
        _commands = list.ToArray();
        _cmdIndex = 0;
    }

    [Benchmark]
    public void ApplyTeleportsBatch()
    {
        // Process a slice of teleports (loop small batch to amortize overhead)
        var len = _commands.Length;
        if (len == 0) return;
        for (int i=0;i<256;i++)
        {
            var cmd = _commands[_cmdIndex];
            _teleport.Apply(in cmd);
            _cmdIndex++; if (_cmdIndex >= len) _cmdIndex = 0;
        }
    }
}
*/
