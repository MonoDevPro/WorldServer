using Arch.Core;
using Simulation.Core.Abstractions.Out;

namespace Simulation.Core.Adapters.Out;

public sealed class EntityIndex : IEntityIndex
{
    private readonly Dictionary<int, Entity> _byCharId = new();
    private readonly object _lock = new();

    public void Register(int characterId, in Entity entity)
    {
        lock (_lock) _byCharId[characterId] = entity;
    }

    public void UnregisterByCharId(int characterId)
    {
        lock (_lock) _byCharId.Remove(characterId);
    }

    public void UnregisterEntity(in Entity entity)
    {
        lock (_lock)
        {
            foreach (var kv in _byCharId.ToArray())
            {
                if (kv.Value == entity) _byCharId.Remove(kv.Key);
            }
        }
    }

    public bool TryGetByCharId(int characterId, out Entity entity)
    {
        lock (_lock) return _byCharId.TryGetValue(characterId, out entity);
    }
}
