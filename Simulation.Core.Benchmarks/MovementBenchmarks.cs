using Arch.Core;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Simulation.Core.Commons;
using Simulation.Core.Components;
using Simulation.Core.Systems;
using Simulation.Core.Utilities;

namespace Simulation.Core.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class MovementBenchmarks
{
    [Params(1_000, 5_000, 10_000)]
    public int Entities;

    private World _world = null!;
    private GridMovementSystem _system = null!;
    private BlockingIndex _blocking = null!;
    private BoundsIndex _bounds = null!;

    [GlobalSetup]
    public void Setup()
    {
        _world = World.Create(Entities + 100);
        _blocking = new BlockingIndex();
        _bounds = new BoundsIndex();
        // Single bounds large enough
        _world.Create(new Bounds{ MinX = -1000, MinY = -1000, MaxX = 1000, MaxY = 1000 }, new MapRef{ MapId = 1 });
        _bounds.RebuildIfDirty(_world);
        var rnd = new Random(42);
        for (int i = 0; i < Entities; i++)
        {
            _world.Create(
                new TilePosition{ Position = new(X: rnd.Next(-50, 50), Y: rnd.Next(-50, 50)) },
                new TileVelocity{ Velocity = new(X: (float)(rnd.NextDouble()*4-2), Y: (float)(rnd.NextDouble()*4-2)) },
                new MapRef{ MapId = 1 });
        }
        _system = new GridMovementSystem(_world, _blocking, _bounds);
    }

    [Benchmark]
    public void Tick60HzFrame()
    {
        _system.Update(1f/60f);
    }
}

