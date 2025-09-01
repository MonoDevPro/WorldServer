using Arch.Core;
using Simulation.Application.Ports.Commons;
using Simulation.Application.Ports.Commons.Indexers;

namespace Simulation.Persistence.Commons;

/// <summary>
/// Implementação genérica de um índice para mapear chaves externas para Entidades.
/// </summary>
public abstract class EntityIndex<TKey> : IEntityIndex<TKey>, IReverseIndex<TKey, Entity>
    where TKey : notnull
{
    private readonly Dictionary<TKey, Entity> _map = new();
    private readonly Dictionary<Entity, TKey> _reverseMap = new();

    public virtual void Register(TKey key, Entity entity)
    {
        if (_reverseMap.TryGetValue(entity, out var existingKey))
        {
            if (!EqualityComparer<TKey>.Default.Equals(existingKey, key))
            {
                _map.Remove(existingKey);
            }
        }

        _map[key] = entity;
        _reverseMap[entity] = key;
    }

    public virtual bool Unregister(TKey key)
    {
        if (_map.Remove(key, out var entity))
        {
            _reverseMap.Remove(entity);
            return true;
        }
        return false;
    }

    public virtual bool UnregisterValue(Entity entity)
    {
        if (_reverseMap.Remove(entity, out var key))
        {
            _map.Remove(key);
            return true;
        }
        return false;
    }

    public virtual bool TryGet(TKey key, out Entity entity) => _map.TryGetValue(key, out entity);

    public virtual bool TryGetKey(Entity entity, out TKey? key) => _reverseMap.TryGetValue(entity, out key);
}