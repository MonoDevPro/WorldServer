using BenchmarkDotNet.Attributes;
using Simulation.Core.Abstractions.Adapters.Data;
using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Utilities;

namespace Simulation.Core.Benchmarks;

public class MapAccessBenchmarks
{
    [Params(64, 512, 1024)]
    public int Size;

    [Params(false, true)]
    public bool UsePadded;

    private MapData _map;
    private Random _rnd;

    [GlobalSetup]
    public void Setup()
    {
        _map = MapData.CreateFromTemplate(0, "MapTest", Size, Size, UsePadded);
        var tilesRow = new TileType[Size*Size];
        var collRow = new byte[Size*Size];
        for(int i=0;i<tilesRow.Length;i++){ tilesRow[i]=TileType.Floor; collRow[i]=0;}
        for(int y=0;y<Size;y+=16) for(int x=0;x<Size;x+=16) collRow[y*Size + x] = 1;
        _map.PopulateFromRowMajor(tilesRow, collRow);
        _rnd = new Random(42);
    }

    [Benchmark(Description="Random IsBlocked")]
    public int RandomIsBlocked()
    {
        int hits = 0;
        int iterations = 1_000_000;
        for(int i=0;i<iterations;i++){
            GameCoord p = new GameCoord(_rnd.Next(Size), _rnd.Next(Size));
            if (_map.IsBlocked(p)) hits++;
        }
        return hits;
    }

    [Benchmark(Description="Sequential Physical Scan")]
    public long SequentialScan()
    {
        long s=0;
        for(int i=0;i<_map.CollisionMask.Length;i++) s += _map.CollisionMask[i];
        return s;
    }
}