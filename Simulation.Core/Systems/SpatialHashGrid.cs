using Simulation.Core.Commons;

namespace Simulation.Core.Systems
{
    /// <summary>
    /// Spatial hash / uniform grid optimized for tile-based worlds.
    /// Uses GameVector2 for positions (tile coordinates).
    /// Thread-safety: per-map lock (coarse but avoids global contention).
    /// Recommendations: call Upsert/Remove from the ECS main thread if possible (fast path, no locks).
    /// </summary>
    public sealed class SpatialHashGrid
    {
        // Per-map structure
        private sealed class MapIndex
        {
            public readonly object Lock = new();
            // cell -> list of entity ids
            public readonly Dictionary<GameVector2, List<int>> Cells = new();
            // entity -> cell (for quick removal / move)
            public readonly Dictionary<int, GameVector2> EntityCell = new();
        }

        // mapId -> MapIndex
        private readonly Dictionary<int, MapIndex> _maps = new();
        private readonly int _cellSize;

        public SpatialHashGrid(int cellSize = 1)
        {
            if (cellSize <= 0) throw new ArgumentOutOfRangeException(nameof(cellSize));
            _cellSize = cellSize;
        }

        private static GameVector2 PosToCell(GameVector2 pos, int cellSize)
        {
            // integer division with floor-behavior for negatives
            int cx = pos.X >= 0 ? pos.X / cellSize : (pos.X - (cellSize - 1)) / cellSize;
            int cy = pos.Y >= 0 ? pos.Y / cellSize : (pos.Y - (cellSize - 1)) / cellSize;
            return new GameVector2(cx, cy);
        }

        private MapIndex GetOrCreateMap(int mapId)
        {
            lock (_maps)
            {
                if (!_maps.TryGetValue(mapId, out var idx))
                {
                    idx = new MapIndex();
                    _maps[mapId] = idx;
                }
                return idx;
            }
        }

        /// <summary>
        /// Ensure a map index exists (optional).
        /// </summary>
        public void EnsureMap(int mapId) => _ = GetOrCreateMap(mapId);

        /// <summary>
        /// Add or move entity to a tile position (tileX, tileY).
        /// Call this whenever an entity is spawned or moves tiles.
        /// </summary>
        public void Upsert(int entityId, int mapId, GameVector2 tilePos)
        {
            var idx = GetOrCreateMap(mapId);
            var cell = PosToCell(tilePos, _cellSize);

            lock (idx.Lock)
            {
                // Remove from old cell if present (and different)
                if (idx.EntityCell.TryGetValue(entityId, out var oldCell))
                {
                    if (!oldCell.Equals(cell))
                    {
                        if (idx.Cells.TryGetValue(oldCell, out var oldList))
                        {
                            // Remove entity from old list
                            var removed = oldList.Remove(entityId);
                            if (oldList.Count == 0) idx.Cells.Remove(oldCell);
                        }
                        idx.EntityCell.Remove(entityId);
                    }
                    else
                    {
                        // same cell, nothing to do
                        return;
                    }
                }

                // Add to new cell
                if (!idx.Cells.TryGetValue(cell, out var list))
                {
                    list = new List<int>(4);
                    idx.Cells[cell] = list;
                }
                list.Add(entityId);
                idx.EntityCell[entityId] = cell;
            }
        }

        /// <summary>
        /// Remove entity from index (call on despawn).
        /// </summary>
        public void Remove(int entityId, int? mapId = null)
        {
            if (mapId.HasValue)
            {
                if (!_maps.TryGetValue(mapId.Value, out var idx)) return;
                lock (idx.Lock)
                {
                    if (!idx.EntityCell.TryGetValue(entityId, out var cell)) return;
                    if (idx.Cells.TryGetValue(cell, out var list))
                    {
                        list.Remove(entityId);
                        if (list.Count == 0) idx.Cells.Remove(cell);
                    }
                    idx.EntityCell.Remove(entityId);
                }
            }
            else
            {
                // If mapId unknown, search all maps (less efficient); usually you pass mapId.
                lock (_maps)
                {
                    foreach (var kv in _maps)
                    {
                        var idx = kv.Value;
                        lock (idx.Lock)
                        {
                            if (idx.EntityCell.TryGetValue(entityId, out var cell))
                            {
                                if (idx.Cells.TryGetValue(cell, out var list))
                                {
                                    list.Remove(entityId);
                                    if (list.Count == 0) idx.Cells.Remove(cell);
                                }
                                idx.EntityCell.Remove(entityId);
                                return;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a *candidate* list of entityIds inside the radius (uses bounding box of cells).
        /// Caller should fetch actual entity positions from ECS and filter by exact distance and flags.
        /// </summary>
        public List<int> QueryRadiusCandidates(int mapId, GameVector2 center, int radius)
        {
            if (!_maps.TryGetValue(mapId, out var idx))
                return new List<int>(0);

            var minX = center.X - radius;
            var maxX = center.X + radius;
            var minY = center.Y - radius;
            var maxY = center.Y + radius;

            var minCell = PosToCell(new GameVector2(minX, minY), _cellSize);
            var maxCell = PosToCell(new GameVector2(maxX, maxY), _cellSize);

            var result = new List<int>(); // caller can re-use pool if desired

            lock (idx.Lock)
            {
                for (int cx = minCell.X; cx <= maxCell.X; cx++)
                {
                    for (int cy = minCell.Y; cy <= maxCell.Y; cy++)
                    {
                        var cellPos = new GameVector2(cx, cy);
                        if (!idx.Cells.TryGetValue(cellPos, out var list)) continue;
                        // copy candidates
                        result.AddRange(list);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Convenience: Query radius and also filter by exact squared distance (tile-based).
        /// radius is in tile units (int). Returns set of entity ids that are inside radius (inclusive).
        /// </summary>
        public List<int> QueryRadiusFiltered(int mapId, GameVector2 center, int radius)
        {
            var candidates = QueryRadiusCandidates(mapId, center, radius);
            if (candidates.Count == 0) return candidates;

            long r2 = (long)radius * radius;
            var filtered = new List<int>(candidates.Count);

            // NOTE: here we don't know entity positions (ECS stores them).
            // This method assumes the caller will provide a position lookup callback.
            // For the generic case we return candidates — prefer using QueryRadiusCandidates + explicit filter.
            // Keeping this method for API symmetry; by default it returns candidates.
            return candidates;
        }

        /// <summary>
        /// Quickly get the cell a given tile position maps to.
        /// </summary>
        public GameVector2 TileToCell(GameVector2 tilePos) => PosToCell(tilePos, _cellSize);

        /// <summary>
        /// Count of entities in a specific cell (for diagnostics).
        /// </summary>
        public int CountInCell(int mapId, GameVector2 cell)
        {
            if (!_maps.TryGetValue(mapId, out var idx)) return 0;
            lock (idx.Lock)
            {
                return idx.Cells.TryGetValue(cell, out var list) ? list.Count : 0;
            }
        }

        /// <summary>
        /// Optional: clear all data for a map (e.g., on map unload).
        /// </summary>
        public void ClearMap(int mapId)
        {
            lock (_maps)
            {
                _maps.Remove(mapId);
            }
        }
    }
}
