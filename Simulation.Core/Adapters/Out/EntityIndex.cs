using System.Collections.Concurrent;
using Arch.Core;
using Simulation.Core.Abstractions.Out;

namespace Simulation.Core.Adapters.Out;

public sealed class EntityIndex : IEntityIndex
{
    private readonly ConcurrentDictionary<int, Entity> _byCharId = new();

    public void Register(int characterId, in Entity entity)
    {
        _byCharId[characterId] = entity;
    }

    public void UnregisterByCharId(int characterId)
    {
        _byCharId.Remove(characterId, out _);
    }

    public void UnregisterEntity(in Entity entity)
    {
        foreach (var kv in _byCharId.ToArray())
            if (kv.Value == entity) _byCharId.Remove(kv.Key, out _);
    }

    public bool TryGetByCharId(int characterId, out Entity entity)
    {
        return _byCharId.TryGetValue(characterId, out entity);
    }
}