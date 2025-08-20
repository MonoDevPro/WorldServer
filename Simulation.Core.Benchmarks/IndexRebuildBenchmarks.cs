using Arch.Core;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Simulation.Core.Components;
using Simulation.Core.Utilities;

namespace Simulation.Core.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class IndexRebuildBenchmarks
{
    [Params(5_000, 20_000, 50_000)]
    public int BlockingTiles;

    private World _world = null!;
    private BlockingIndex _blocking = null!;
    private BoundsIndex _bounds = null!;

    [GlobalSetup]
    public void Setup()
    {
        _world = World.Create(BlockingTiles + 10);
        _blocking = new BlockingIndex();
        _bounds = new BoundsIndex();
        // one bounds entity
        _world.Create(new Bounds{ MinX=-5000, MinY=-5000, MaxX=5000, MaxY=5000}, new MapRef{ MapId = 1 });
        var rnd = new Random(77);
        for (int i=0;i<BlockingTiles;i++)
        {
            _world.Create(new Blocking(), new TilePosition{ Position = new (X: rnd.Next(-1000,1001), Y: rnd.Next(-1000,1001))}, new MapRef{ MapId = 1 });
        }
    }

    [Benchmark]
    public void RebuildBlockingIndex()
    {
        _blocking.MarkDirty();
        _blocking.RebuildIfDirty(_world);
    }

    [Benchmark]
    public void RebuildBoundsIndex()
    {
        _bounds.MarkDirty();
        _bounds.RebuildIfDirty(_world);
    }
}

