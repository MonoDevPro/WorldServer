using System.Collections.Concurrent;
using Arch.Core;
using Simulation.Core.Abstractions.Out;

namespace Simulation.Core.Utilities;

public sealed class EntityIndex : IEntityIndex
{
    // charId -> Entity (value contains Version/WorldId)
    private readonly ConcurrentDictionary<int, Entity> _byCharId = new();

    // entityId -> charId (fast inverse lookup to allow O(1) UnregisterEntity)
    private readonly ConcurrentDictionary<int, int> _entityToChar = new();

    public void Register(int characterId, in Entity entity)
    {
        // upsert char->entity
        _byCharId[characterId] = entity;
        // upsert entity->char
        _entityToChar[entity.Id] = characterId;
    }

    public void UnregisterByCharId(int characterId)
    {
        if (_byCharId.TryRemove(characterId, out var ent))
        {
            _entityToChar.TryRemove(ent.Id, out _);
        }
    }

    public void UnregisterEntity(in Entity entity)
    {
        // fastest path: lookup inverse map
        if (_entityToChar.TryRemove(entity.Id, out var charId))
        {
            // attempt remove by charId (best-effort)
            _byCharId.TryRemove(charId, out _);
            return;
        }

        // fallback (rare): scan by value (only if inverse map missing)
        foreach (var kv in _byCharId)
        {
            if (kv.Value.Equals(entity))
            {
                _byCharId.TryRemove(kv.Key, out _);
                // also try cleanup inverse map
                _entityToChar.TryRemove(entity.Id, out _);
                break;
            }
        }
    }

    public bool TryGetByCharId(int characterId, out Entity entity)
    {
        return _byCharId.TryGetValue(characterId, out entity);
    }

    // utilitário: obter por entityId
    public bool TryGetByEntityId(int entityId, out Entity entity)
    {
        entity = default;
        if (_entityToChar.TryGetValue(entityId, out var charId))
        {
            return _byCharId.TryGetValue(charId, out entity);
        }
        return false;
    }
}
