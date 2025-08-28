// Simulation.Core.Spatial/SpatialHashGrid.cs
// Adaptação do seu código: adiciona pool para overflow lists, micro-optimizações e método RemoveAllAddsThenInserts.

using System;
using System.Buffers;
using System.Collections.Generic;
using Arch.LowLevel.Jagged;
using Simulation.Core.Abstractions;
using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Core.Utilities;

public class SpatialIndex : ISpatialIndex
{
    private readonly IMapIndex _mapIndex;
    
    private readonly int _bucketSize;
    private readonly Dictionary<int, JaggedArray<int>> _mapBuckets = new();
    private readonly Dictionary<int, Dictionary<int, List<int>>> _overflow = new();
    private readonly Dictionary<int, int> _entityToSlot = new();
    private readonly Dictionary<int, int> _entityToMap = new();
    private readonly Stack<List<int>> _listPool = new();
    private const int FILLER = -1;

    private readonly Dictionary<int, (int mapId, GameCoord oldP, GameCoord newP)> _pending =
        new();

    private bool _disposed = false;

    public SpatialIndex(IMapIndex mapIndex, int bucketSize = 8)
    {
        _mapIndex = mapIndex ?? throw new ArgumentNullException(nameof(mapIndex));
        if (bucketSize <= 0) throw new ArgumentOutOfRangeException(nameof(bucketSize));
        _bucketSize = bucketSize;
    }

    private void EnsureMapInitialized(int mapId)
    {
        if (_mapBuckets.ContainsKey(mapId)) return;
        if (!_mapIndex.TryGetMap(mapId, out var map))
            throw new InvalidOperationException($"Map {mapId} not found in MapIndex.");

        var totalSlots = map.Count * _bucketSize;
        var jagged = new JaggedArray<int>(_bucketSize, FILLER, totalSlots);
        _mapBuckets[mapId] = jagged;
        _overflow[mapId] = new Dictionary<int, List<int>>();
    }

    private List<int> RentList()
    {
        return _listPool.Count > 0 ? _listPool.Pop() : new List<int>(4);
    }

    private void ReturnList(List<int> list)
    {
        list.Clear();
        _listPool.Push(list);
    }

    public void Register(int entityId, int mapId, GameCoord tilePos)
    {
        EnsureMapInitialized(mapId);

        var map = _mapIndex.TryGetMap(mapId, out var m) ? m! : throw new InvalidOperationException($"Map {mapId} not found.");
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
            list = RentList();
            mapOverflow[storageIndex] = list;
        }

        list.Add(entityId);
        _entityToMap[entityId] = mapId;
        _entityToSlot[entityId] = -(storageIndex + 1);
    }

    public void EnqueueUpdate(int entityId, int mapId, GameCoord oldPos, GameCoord newPos)
    {
        _pending[entityId] = (mapId, oldPos, newPos);
    }

    public void Flush()
    {
        if (_pending.Count == 0) return;

        // strategy: removes first then adds (to reduce conflicts)
        var removes = new List<int>();
        var adds = new List<(int entityId, int mapId, GameCoord pos)>();

        foreach (var kv in _pending)
        {
            var entityId = kv.Key;
            var (mapId, oldP, newP) = kv.Value;
            if (oldP == newP) continue;

            removes.Add(entityId);
            adds.Add((entityId, mapId, newP));
        }

        // removes
        foreach (var entityId in removes)
        {
            if (_entityToSlot.TryGetValue(entityId, out var sVal))
            {
                if (sVal >= 0)
                {
                    // find which map it currently belongs to
                    if (_entityToMap.TryGetValue(entityId, out var mId) && _mapBuckets.TryGetValue(mId, out var jagged))
                    {
                        jagged.Remove(sVal);
                    }
                    _entityToSlot.Remove(entityId);
                }
                else
                {
                    int oldStorage = -sVal - 1;
                    if (_entityToMap.TryGetValue(entityId, out var mId) && _overflow.TryGetValue(mId, out var mapOverflow) && mapOverflow.TryGetValue(oldStorage, out var list))
                    {
                        list.Remove(entityId);
                        if (list.Count == 0)
                        {
                            mapOverflow.Remove(oldStorage);
                            ReturnList(list);
                        }
                    }
                    _entityToSlot.Remove(entityId);
                }
            }
            if (_entityToMap.ContainsKey(entityId)) _entityToMap.Remove(entityId);
        }

        // adds
        foreach (var add in adds)
        {
            Register(add.entityId, add.mapId, add.pos);
        }

        _pending.Clear();
    }

    public void UpdatePosition(int entityId, int mapId, GameCoord oldPos, GameCoord newPos)
    {
        // Immediate non-batched fallback
        if (_entityToSlot.TryGetValue(entityId, out var sVal))
        {
            if (sVal >= 0)
            {
                if (_mapBuckets.TryGetValue(mapId, out var jagged)) jagged.Remove(sVal);
                _entityToSlot.Remove(entityId);
            }
            else
            {
                int oldStorage = -sVal - 1;
                if (_overflow.TryGetValue(mapId, out var mapOverflow) && mapOverflow.TryGetValue(oldStorage, out var list))
                {
                    list.Remove(entityId);
                    if (list.Count == 0)
                    {
                        mapOverflow.Remove(oldStorage);
                        ReturnList(list);
                    }
                }
                _entityToSlot.Remove(entityId);
            }
        }

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
                    if (list.Count == 0)
                    {
                        mo.Remove(storageIndex);
                        ReturnList(list);
                    }
                }
                _entityToSlot.Remove(entityId);
            }
        }

        _entityToMap.Remove(entityId);
        _pending.Remove(entityId);
    }

    public void QueryAABB(int mapId, int minX, int minY, int maxX, int maxY, Action<int> visitor)
    {
        if (!_mapBuckets.TryGetValue(mapId, out var jagged))
        {
            if (!_mapIndex.TryGetMap(mapId, out var _)) return;
            EnsureMapInitialized(mapId);
            jagged = _mapBuckets[mapId];
        }

        if (!_mapIndex.TryGetMap(mapId, out var map)) return;

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

    public void QueryRadius(int mapId, GameCoord center, int radius, Action<int> visitor)
    {
        if (!_mapIndex.TryGetMap(mapId, out var map)) return;
        var minX = center.X - radius;
        var minY = center.Y - radius;
        var maxX = center.X + radius;
        var maxY = center.Y + radius;
        QueryAABB(mapId, minX, minY, maxX, maxY, visitor);
    }

    public bool IsRegistered(int entityId) => _entityToMap.ContainsKey(entityId);
    public int? GetEntityMap(int entityId) => _entityToMap.TryGetValue(entityId, out var m) ? m : (int?)null;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            foreach (var jaggedArray in _mapBuckets.Values)
                jaggedArray?.Clear();
            _mapBuckets.Clear();

            foreach (var mo in _overflow.Values)
            {
                foreach (var list in mo.Values)
                {
                    list.Clear();
                    _listPool.Push(list);
                }
            }
            _overflow.Clear();
            _entityToSlot.Clear();
            _entityToMap.Clear();
            _pending.Clear();
        }

        _disposed = true;
    }

    ~SpatialIndex()
    {
        Dispose(false);
    }
}