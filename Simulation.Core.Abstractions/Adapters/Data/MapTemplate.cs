using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.Adapters.Data;

public enum TileType : byte { Empty = 0, Floor = 1, Wall = 2, TreeStump = 3 }

public class MapTemplate
{
    public string? Name { get; set; } = string.Empty;
    // row-major arrays: length = Width * Height
    public TileType[]? TilesRowMajor { get; set; }
    public byte[]? CollisionRowMajor { get; set; }
    // ECS Identifiers
    public MapId MapId { get; set; } = new(0);
    public MapSize MapSize { get; set; } = new(new GameSize(0,0));
    public MapFlags Flags { get; set; } = new(UsePadded: false);
}