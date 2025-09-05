using System.Collections.Concurrent;
using Arch.Core;
using Simulation.Application.Ports.Commons;
using Simulation.Application.Ports.ECS.Utils.Indexers;

namespace Simulation.Application.Services.ECS.Utils.Indexers;

/// <summary>
/// Implementação genérica de um índice para mapear chaves externas para Entidades.
/// </summary>
public abstract class EntityIndex<TKey> : IEntityIndex<TKey>, IReverseIndex<TKey, Entity>
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Entity> _map = new();
    private readonly ConcurrentDictionary<Entity, TKey> _reverseMap = new();

    public virtual void Register(TKey key, Entity entity)
    {
        if (_reverseMap.TryGetValue(entity, out var existingKey))
        {
            if (!EqualityComparer<TKey>.Default.Equals(existingKey, key))
            {
                _map.TryRemove(existingKey, out _);
            }
        }

        _map[key] = entity;
        _reverseMap[entity] = key;
    }

    public virtual bool Unregister(TKey key)
    {
        if (_map.Remove(key, out var entity))
        {
            _reverseMap.TryRemove(entity, out _);
            return true;
        }
        return false;
    }

    public virtual bool UnregisterValue(Entity entity)
    {
        if (_reverseMap.TryRemove(entity, out var key))
        {
            _map.TryRemove(key, out _);
            return true;
        }
        return false;
    }

    public virtual bool TryGet(TKey key, out Entity entity) => _map.TryGetValue(key, out entity);

    public virtual bool TryGetKey(Entity entity, out TKey? key) => _reverseMap.TryGetValue(entity, out key);
}