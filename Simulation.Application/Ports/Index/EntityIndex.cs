using Arch.Core;

namespace Simulation.Application.Ports.Index;

/// <summary>
/// Implementação genérica de um índice para mapear chaves externas para Entidades.
/// </summary>
public abstract class EntityIndex<TKey> : IEntityIndex<TKey> where TKey : notnull
{
    private readonly Dictionary<TKey, Entity> _map = new();
    private readonly Dictionary<Entity, TKey> _reverseMap = new(); // Para remoção eficiente

    public virtual void Register(TKey key, Entity entity)
    {
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
        
    public virtual bool Unregister(Entity entity)
    {
        if (_reverseMap.Remove(entity, out var key))
        {
            _map.Remove(key);
            return true;
        }
        return false;
    }

    public virtual bool TryGet(TKey key, out Entity entity) => _map.TryGetValue(key, out entity);
}
