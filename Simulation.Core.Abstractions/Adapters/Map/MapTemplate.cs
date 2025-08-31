using LiteNetLib.Utils;

namespace Simulation.Core.Abstractions.Adapters.Map;

public enum TileType : byte { Empty = 0, Floor = 1, Wall = 2, TreeStump = 3 }

public class MapTemplate : INetSerializable
{
    public string? Name { get; set; } = string.Empty;
    // row-major arrays: length = Width * Height
    public TileType[]? TilesRowMajor { get; set; }
    public byte[]? CollisionRowMajor { get; set; }
    // ECS Identifiers
    public int MapId { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool UsePadded { get; set; } = false;
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(MapId);
        writer.Put(Width);
        writer.Put(Height);
        writer.Put(UsePadded);
        writer.Put(Name ?? string.Empty);
        if (TilesRowMajor == null || CollisionRowMajor == null || TilesRowMajor.Length != Width * Height || CollisionRowMajor.Length != Width * Height)
            throw new InvalidOperationException("TilesRowMajor and CollisionRowMajor must be non-null and match Width*Height");
        writer.PutArray(Array.ConvertAll(TilesRowMajor, t => (int)t));
        writer.PutArray(Array.ConvertAll(CollisionRowMajor, b => (int)b));
    }

    public void Deserialize(NetDataReader reader)
    {
        MapId = reader.GetInt();
        Width = reader.GetInt();
        Height = reader.GetInt();
        UsePadded = reader.GetBool();
        Name = reader.GetString();
        TilesRowMajor = Array.ConvertAll(reader.GetIntArray(), t => (TileType)t);
        CollisionRowMajor = Array.ConvertAll(reader.GetIntArray(), b => (byte)b);
        if (TilesRowMajor == null || CollisionRowMajor == null || TilesRowMajor.Length != Width * Height || CollisionRowMajor.Length != Width * Height)
            throw new InvalidOperationException("TilesRowMajor and CollisionRowMajor must be non-null and match Width*Height");
    }
}