using Simulation.Application.Ports.Commons;

namespace Simulation.Application.Services.Commons;

public abstract class DefaultIndex<TKey, TValue> : IIndex<TKey, TValue>, IReverseIndex<TKey, TValue>
    where TKey : notnull
    where TValue : notnull
{
    private readonly Dictionary<TKey, TValue> _map = new();
    private readonly Dictionary<TValue, TKey> _reverseMap = new();

    public virtual void Register(TKey key, TValue value)
    {
        // se o value já existe ligado a outra key, atualiza reverseMap corretamente
        if (_reverseMap.TryGetValue(value, out var existingKey))
        {
            if (!EqualityComparer<TKey>.Default.Equals(existingKey, key))
            {
                _map.Remove(existingKey);
            }
        }
        _map[key] = value;
        _reverseMap[value] = key;
    }

    public virtual bool Unregister(TKey key)
    {
        if (_map.Remove(key, out var value))
        {
            _reverseMap.Remove(value);
            return true;
        }
        return false;
    }

    public virtual bool UnregisterValue(TValue value)
    {
        if (_reverseMap.Remove(value, out var key))
        {
            _map.Remove(key);
            return true;
        }
        return false;
    }

    public virtual bool TryGet(TKey key, out TValue? value) => _map.TryGetValue(key, out value);

    public virtual bool TryGetKey(TValue value, out TKey? key) => _reverseMap.TryGetValue(value, out key);
}