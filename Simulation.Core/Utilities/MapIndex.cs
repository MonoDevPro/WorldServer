using Simulation.Core.Abstractions.Commons.VOs;

namespace Simulation.Core.Utilities;

public class MapIndex
{
    private static readonly Dictionary<int, MapData> Maps = new();

    public static void Add(int mapId, MapData map) => Maps[mapId] = map;
    public static bool TryGetMap(int mapId, out MapData? mapData) => Maps.TryGetValue(mapId, out mapData);
}

// DTO simples para persistência (row-major)
public class MapDto
{
    public int MapId { get; set; }
    public string? Name { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool UsePadded { get; set; }

    // row-major arrays: length = Width * Height
    public MapData.TileType[]? TilesRowMajor { get; set; }
    public byte[]? CollisionRowMajor { get; set; }
}

public class MapData
{
    public enum TileType : byte { Empty = 0, Floor = 1, Wall = 2, TreeStump = 3 }

    public int MapId { get; init; }
    public string Name { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public int Count => Width * Height;

    // storage mode
    public bool UsePadded { get; init; } = false;
    public int PaddedSize { get; init; } = 0; // p (if padded)

    // mapping (used for compact mode)
    // posToRank: index by linear pos (y*width + x) -> rank (0..Count-1)
    private readonly int[]? _posToRank;
    private readonly (int x, int y)[]? _rankToPos;

    // storage arrays: ALWAYS kept in Morton order (rank order) or padded order
    public TileType[] Tiles { get; private set; }
    public byte[] CollisionMask { get; private set; } // 0=free, 1=blocked

    private MapData(int mapId, string name, int width, int height, bool usePadded)
    {
        MapId = mapId;
        Name = name ?? string.Empty;
        Width = width;
        Height = height;
        UsePadded = usePadded;

        if (usePadded)
        {
            int p = Morton.NextPow2(Math.Max(width, height));
            PaddedSize = p;
            long pCount = (long)p * (long)p;
            if (pCount > int.MaxValue) throw new ArgumentException("padded size too large");
            Tiles = new TileType[p * p];
            CollisionMask = new byte[p * p];
            _posToRank = null;
            _rankToPos = null;
        }
        else
        {
            (int[] posToRank, (int x, int y)[] rankToPos) = Morton.BuildMortonMapping(width, height);
            _posToRank = posToRank;
            _rankToPos = rankToPos;
            Tiles = new TileType[width * height];
            CollisionMask = new byte[width * height];
        }
    }

    // Factory
    public static MapData Create(int mapId, string name, int width, int height, bool usePadded = false)
    {
        if (width <= 0 || height <= 0) throw new ArgumentException("invalid size");
        return new MapData(mapId, name ?? string.Empty, width, height, usePadded);
    }
    public static MapData CreateFromDto(MapDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        var map = new MapData(dto.MapId, dto.Name ?? string.Empty, dto.Width, dto.Height, dto.UsePadded);
        if (dto.TilesRowMajor != null)
            map.PopulateFromRowMajor(dto.TilesRowMajor, dto.CollisionRowMajor);
        return map;
    }

    // helpers
    private int LinearPos(int x, int y) => y * Width + x;
    private int LinearPos(GameVector2 p) => p.Y * Width + p.X;

    // converts (x,y) -> storage index (rank or padded idx)
    public int StorageIndex(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) throw new ArgumentOutOfRangeException();
        if (UsePadded)
        {
            // padded: Morton code fits in padded square
            ulong idx = Morton.MortonIndexPadded(x, y, Width, Height);
            return (int)idx;
        }
        else
        {
            int pos = LinearPos(x, y);
            return _posToRank![pos];
        }
    }

    public int StorageIndex(GameVector2 p)
    {
        return StorageIndex(p.X, p.Y);
    }

    // accessors
    public TileType GetTile(GameVector2 p)
    {
        if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
        return Tiles[StorageIndex(p)];
    }

    public void SetTile(GameVector2 p, TileType t)
    {
        if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
        Tiles[StorageIndex(p)] = t;
    }

    public bool IsBlocked(GameVector2 p)
    {
        if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
        return CollisionMask[StorageIndex(p)] != 0;
    }

    public void SetBlocked(GameVector2 p, bool blocked)
    {
        if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
        CollisionMask[StorageIndex(p)] = blocked ? (byte)1 : (byte)0;
    }

    public bool InBounds(GameVector2 p)
        => (uint)p.X < (uint)Width && (uint)p.Y < (uint)Height;

    // Optional: get coordinates from storage index (only meaningful for compact mode)
    public (int x, int y) CoordFromRank(int rank)
    {
        if (UsePadded) throw new InvalidOperationException("CoordFromRank is only valid for compact mode");
        if (rank < 0 || rank >= Count) throw new ArgumentOutOfRangeException(nameof(rank));
        return _rankToPos![rank];
    }

    // helper: fill from row-major input arrays (useful when loading map data)
    // Input arrays are assumed row-major size width*height.
    public void PopulateFromRowMajor(TileType[] tilesRowMajor, byte[] collisionRowMajor)
    {
        if (tilesRowMajor == null) throw new ArgumentNullException(nameof(tilesRowMajor));
        if (tilesRowMajor.Length != Width * Height) throw new ArgumentException("tiles length mismatch");
        if (collisionRowMajor != null && collisionRowMajor.Length != Width * Height) throw new ArgumentException("collision length mismatch");

        if (UsePadded)
        {
            // copy each (x,y) to padded storage using Morton index
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int srcPos = y * Width + x;
                    int dst = (int)Morton.MortonIndexPadded(x, y, Width, Height);
                    Tiles[dst] = tilesRowMajor[srcPos];
                    if (collisionRowMajor != null) CollisionMask[dst] = collisionRowMajor[srcPos];
                }
            }
        }
        else
        {
            // compact: use pos->rank mapping
            for (int pos = 0; pos < Width * Height; pos++)
            {
                int r = _posToRank![pos];
                Tiles[r] = tilesRowMajor[pos];
                if (collisionRowMajor != null) CollisionMask[r] = collisionRowMajor[pos];
            }
        }
    }

    // small convenience: iterate neighbors in Manhattan 4-neighborhood
    public IEnumerable<(int x, int y)> Neighbors4(int x, int y)
    {
        if (x > 0) yield return (x - 1, y);
        if (x + 1 < Width) yield return (x + 1, y);
        if (y > 0) yield return (x, y - 1);
        if (y + 1 < Height) yield return (x, y + 1);
    }
}

public static class Morton
{
    // --- Helpers (64-bit Morton for up to 32-bit x,y) ---
    // Interleave lower 32 bits of x so there is one 0 bit between each original bit.
    private static ulong Part1By1(ulong x)
    {
        // masks for 64-bit interleave technique
        x &= 0x00000000FFFFFFFFUL;
        x = (x | (x << 16)) & 0x0000FFFF0000FFFFUL;
        x = (x | (x << 8))  & 0x00FF00FF00FF00FFUL;
        x = (x | (x << 4))  & 0x0F0F0F0F0F0F0F0FUL;
        x = (x | (x << 2))  & 0x3333333333333333UL;
        x = (x | (x << 1))  & 0x5555555555555555UL;
        return x;
    }

    // compact every second bit to the right (reverse of Part1By1)
    private static ulong Compact1By1(ulong x)
    {
        x &= 0x5555555555555555UL;
        x = (x | (x >> 1)) & 0x3333333333333333UL;
        x = (x | (x >> 2)) & 0x0F0F0F0F0F0F0F0FUL;
        x = (x | (x >> 4)) & 0x00FF00FF00FF00FFUL;
        x = (x | (x >> 8)) & 0x0000FFFF0000FFFFUL;
        x = (x | (x >> 16)) & 0x00000000FFFFFFFFUL;
        return x;
    }

    /// <summary>
    /// Encode (x,y) into a 64-bit Morton code (Z-order). Accepts 0 <= x,y < 2^32.
    /// </summary>
    public static ulong Encode(uint x, uint y)
    {
        return Part1By1(x) | (Part1By1(y) << 1);
    }

    /// <summary>
    /// Decode a 64-bit Morton code into (x,y).
    /// </summary>
    public static void Decode(ulong code, out uint x, out uint y)
    {
        x = (uint)Compact1By1(code);
        y = (uint)Compact1By1(code >> 1);
    }

    // --- Utilities for arrays ---

    /// <summary>
    /// Next power of two >= v.
    /// </summary>
    public static int NextPow2(int v)
    {
        if (v <= 0) return 1;
        v--;
        v |= v >> 1;
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        v++;
        return v;
    }

    /// <summary>
    /// Get a Morton-based index inside a padded square array of size p x p,
    /// where p = next power-of-two(max(width,height)).
    /// The returned index is in [0, p*p-1]. You must allocate array of length p*p.
    /// This is the simplest approach (fast lookup), but wastes memory for non-power-of-two sizes.
    /// </summary>
    public static ulong MortonIndexPadded(int x, int y, int width, int height)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
            throw new ArgumentOutOfRangeException(nameof(x), "x,y must be inside map bounds");

        int p = NextPow2(Math.Max(width, height));
        // encode using x,y as unsigned
        return Encode((uint)x, (uint)y); // result < p*p because p is power of two of needed bits
    }

    /// <summary>
    /// Build a mapping from (x,y) to dense rank [0..width*height-1] following Z-order but without padding waste.
    /// Returns two arrays:
    /// - posToRank[pos] = rank
    /// - rankToPos[rank] = (x,y)
    /// where pos = y*width + x
    /// </summary>
    public static (int[] posToRank, (int x, int y)[] rankToPos) BuildMortonMapping(int width, int height)
    {
        if (width <= 0 || height <= 0) throw new ArgumentException("width/height > 0");

        int n = width * height;
        var arr = new (ulong key, int pos)[n];
        int idx = 0;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            ulong key = Encode((uint)x, (uint)y);
            arr[idx++] = (key, y * width + x);
        }

        // sort by Morton key
        Array.Sort(arr, (a, b) => a.key.CompareTo(b.key));

        var posToRank = new int[n];
        var rankToPos = new (int x, int y)[n];
        for (int rank = 0; rank < n; rank++)
        {
            int pos = arr[rank].pos;
            posToRank[pos] = rank;
            int x = pos % width;
            int y = pos / width;
            rankToPos[rank] = (x, y);
        }

        return (posToRank, rankToPos);
    }

    // helper to pack/unpack if needed:
    public static int PackXY(int x, int y) => (x & 0xFFFF) | (y << 16);
    public static (int x, int y) UnpackXY(int packed) => (packed & 0xFFFF, packed >> 16);
}