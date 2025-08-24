using Arch.LowLevel.Jagged;
using Simulation.Core.Abstractions.Commons.VOs;
using Simulation.Core.Abstractions.Out;

namespace Simulation.Core.Utilities;

public interface ISpatialIndex
{
    void Register(int entityId, int mapId, GameVector2 tilePos);
    void UpdatePosition(int entityId, int mapId, GameVector2 oldPos, GameVector2 newPos);
    void Unregister(int entityId, int mapId);
    void QueryAABB(int mapId, int minX, int minY, int maxX, int maxY, Action<int> visitor);
    void QueryRadius(int mapId, GameVector2 center, int radius, Action<int> visitor);

    // New API
    bool IsRegistered(int entityId);
    int? GetEntityMap(int entityId);

    // Batch (pending) API
    void EnqueueUpdate(int entityId, int mapId, GameVector2 oldPos, GameVector2 newPos);
    void Flush(); // apply all enqueued updates
}

public class SpatialHashGrid : ISpatialIndex
{
    private readonly int _bucketSize;

    private readonly Dictionary<int, JaggedArray<int>> _mapBuckets = new();
    private readonly Dictionary<int, Dictionary<int, List<int>>> _overflow = new();
    private readonly Dictionary<int, int> _entityToSlot = new();
    private readonly Dictionary<int, int> _entityToMap = new();

    private const int FILLER = -1;

    // pending updates: entityId -> (mapId, old, @new)
    private readonly Dictionary<int, (int mapId, GameVector2 oldP, GameVector2 newP)> _pending =
        new Dictionary<int, (int, GameVector2, GameVector2)>();

    public SpatialHashGrid(IEntityIndex entityIndex, int bucketSize = 8)
    {
        if (bucketSize <= 0) throw new ArgumentOutOfRangeException(nameof(bucketSize));
        _bucketSize = bucketSize;
    }

    private void EnsureMapInitialized(int mapId)
    {
        if (_mapBuckets.ContainsKey(mapId)) return;
        if (!MapIndex.TryGetMap(mapId, out var map))
            throw new InvalidOperationException($"Map {mapId} not found in MapIndex.");

        var totalSlots = map.Count * _bucketSize;
        var jagged = new JaggedArray<int>(_bucketSize, FILLER, totalSlots);
        _mapBuckets[mapId] = jagged;
        _overflow[mapId] = new Dictionary<int, List<int>>();
    }

    public void Register(int entityId, int mapId, GameVector2 tilePos)
    {
        EnsureMapInitialized(mapId);
        var map = MapIndex.TryGetMap(mapId, out var m) ? m! : throw new InvalidOperationException($"Map {mapId} not found.");
        int storageIndex = map.StorageIndex(tilePos);
        var jagged = _mapBuckets[mapId];
        int baseGlobal = storageIndex * _bucketSize;

        for (int slot = 0; slot < _bucketSize; slot++)
        {
            int globalIndex = baseGlobal + slot;
            if (!jagged.ContainsKey(globalIndex))
            {
                jagged.Add(globalIndex, entityId);
                _entityToSlot[entityId] = globalIndex;
                _entityToMap[entityId] = mapId;
                return;
            }
        }

        // overflow fallback
        if (!_overflow.TryGetValue(mapId, out var mapOverflow))
        {
            mapOverflow = new Dictionary<int, List<int>>();
            _overflow[mapId] = mapOverflow;
        }

        if (!mapOverflow.TryGetValue(storageIndex, out var list))
        {
            list = new List<int>(4);
            mapOverflow[storageIndex] = list;
        }

        list.Add(entityId);
        _entityToMap[entityId] = mapId;
        _entityToSlot[entityId] = -(storageIndex + 1); // negative => overflow
    }

    public void EnqueueUpdate(int entityId, int mapId, GameVector2 oldPos, GameVector2 newPos)
    {
        // Overwrite pending if already exists — last write wins (typical for movement)
        _pending[entityId] = (mapId, oldPos, newPos);
        // do not apply yet
    }

    public void Flush()
    {
        if (_pending.Count == 0) return;

        // Optionally: we could reorder operations here (all removes then all adds), but we'll do per-entity remove->add for simplicity.
        foreach (var kv in _pending)
        {
            var entityId = kv.Key;
            var (mapId, oldP, newP) = kv.Value;

            // If same tile, skip
            if (oldP == newP) continue;

            // Make sure map exists/initialized
            if (!_mapBuckets.ContainsKey(mapId))
            {
                try { EnsureMapInitialized(mapId); } catch { continue; }
            }

            // Remove from old storage (if present)
            if (_entityToSlot.TryGetValue(entityId, out var sVal))
            {
                if (sVal >= 0)
                {
                    // main bucket case
                    var jagged = _mapBuckets[mapId];
                    jagged.Remove(sVal);
                    _entityToSlot.Remove(entityId);
                }
                else
                {
                    // overflow encoding
                    int oldStorage = -sVal - 1;
                    if (_overflow.TryGetValue(mapId, out var mapOverflow) && mapOverflow.TryGetValue(oldStorage, out var list))
                    {
                        list.Remove(entityId);
                        if (list.Count == 0) mapOverflow.Remove(oldStorage);
                    }
                    _entityToSlot.Remove(entityId);
                }
            }
            else
            {
                // entity might not be registered yet (spawn during frame). We still proceed to register below.
            }

            // Insert into new storage (try main bucket)
            var map = MapIndex.TryGetMap(mapId, out var m) ? m! : null;
            if (map == null) continue;
            int newStorage = map.StorageIndex(newP);
            var jag = _mapBuckets[mapId];
            int baseGlobal = newStorage * _bucketSize;
            bool placed = false;
            for (int slot = 0; slot < _bucketSize; slot++)
            {
                int g = baseGlobal + slot;
                if (!jag.ContainsKey(g))
                {
                    jag.Add(g, entityId);
                    _entityToSlot[entityId] = g;
                    _entityToMap[entityId] = mapId;
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                // overflow fallback
                if (!_overflow.TryGetValue(mapId, out var mo))
                {
                    mo = new Dictionary<int, List<int>>();
                    _overflow[mapId] = mo;
                }

                if (!mo.TryGetValue(newStorage, out var list))
                {
                    list = new List<int>(4);
                    mo[newStorage] = list;
                }
                list.Add(entityId);
                _entityToSlot[entityId] = -(newStorage + 1);
                _entityToMap[entityId] = mapId;
            }
        }

        // Clear pending
        _pending.Clear();
    }

    public void UpdatePosition(int entityId, int mapId, GameVector2 oldPos, GameVector2 newPos)
    {
        // Backwards compatible: immediate update (non-batch)
        // Remove old
        if (_entityToSlot.TryGetValue(entityId, out var sVal))
        {
            if (sVal >= 0)
            {
                var jagged = _mapBuckets[mapId];
                jagged.Remove(sVal);
                _entityToSlot.Remove(entityId);
            }
            else
            {
                int oldStorage = -sVal - 1;
                if (_overflow.TryGetValue(mapId, out var mapOverflow) && mapOverflow.TryGetValue(oldStorage, out var list))
                {
                    list.Remove(entityId);
                    if (list.Count == 0) mapOverflow.Remove(oldStorage);
                }
                _entityToSlot.Remove(entityId);
            }
        }

        // Add new (simple reuse of Register behavior)
        Register(entityId, mapId, newPos);
    }

    public void Unregister(int entityId, int mapId)
    {
        if (_entityToSlot.TryGetValue(entityId, out var slotVal))
        {
            if (slotVal >= 0)
            {
                if (_mapBuckets.TryGetValue(mapId, out var jagged))
                {
                    jagged.Remove(slotVal);
                }
                _entityToSlot.Remove(entityId);
            }
            else
            {
                int storageIndex = -slotVal - 1;
                if (_overflow.TryGetValue(mapId, out var mo) && mo.TryGetValue(storageIndex, out var list))
                {
                    list.Remove(entityId);
                    if (list.Count == 0) mo.Remove(storageIndex);
                }
                _entityToSlot.Remove(entityId);
            }
        }

        if (_entityToMap.ContainsKey(entityId)) _entityToMap.Remove(entityId);
        if (_pending.ContainsKey(entityId)) _pending.Remove(entityId);
    }

    public void QueryAABB(int mapId, int minX, int minY, int maxX, int maxY, Action<int> visitor)
    {
        if (!_mapBuckets.TryGetValue(mapId, out var jagged))
        {
            if (!MapIndex.TryGetMap(mapId, out var _)) return;
            EnsureMapInitialized(mapId);
            jagged = _mapBuckets[mapId];
        }

        if (!MapIndex.TryGetMap(mapId, out var map)) return;

        minX = Math.Max(minX, 0);
        minY = Math.Max(minY, 0);
        maxX = Math.Min(maxX, map.Width - 1);
        maxY = Math.Min(maxY, map.Height - 1);

        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            var storageIndex = map.StorageIndex(x, y);
            int baseGlobal = storageIndex * _bucketSize;
            for (int slot = 0; slot < _bucketSize; slot++)
            {
                int g = baseGlobal + slot;
                if (jagged.TryGetValue(g, out int eid))
                {
                    visitor(eid);
                }
            }

            if (_overflow.TryGetValue(mapId, out var mo) && mo.TryGetValue(storageIndex, out var ol))
            {
                for (int i = 0; i < ol.Count; i++) visitor(ol[i]);
            }
        }
    }

    public void QueryRadius(int mapId, GameVector2 center, int radius, Action<int> visitor)
    {
        if (!MapIndex.TryGetMap(mapId, out var map)) return;
        var minX = center.X - radius;
        var minY = center.Y - radius;
        var maxX = center.X + radius;
        var maxY = center.Y + radius;
        QueryAABB(mapId, minX, minY, maxX, maxY, visitor);
    }

    // New small API:
    public bool IsRegistered(int entityId) => _entityToMap.ContainsKey(entityId);
    public int? GetEntityMap(int entityId) => _entityToMap.TryGetValue(entityId, out var m) ? m : (int?)null;
}