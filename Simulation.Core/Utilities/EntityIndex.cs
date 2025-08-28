using Arch.Core;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Core.Utilities;

    
public sealed class EntityIndex : IEntityIndex
{
    private readonly Dictionary<int, Entity> _byCharId = new();
    private readonly Dictionary<int, int> _entityToChar = new();

    public void Register(int characterId, in Entity entity)
    {
        _byCharId[characterId] = entity;
        _entityToChar[entity.Id] = characterId;
    }

    public void UnregisterByCharId(int characterId)
    {
        if (_byCharId.Remove(characterId, out var ent))
        {
            _entityToChar.Remove(ent.Id);
        }
    }

    public void UnregisterEntity(in Entity entity)
    {
        if (_entityToChar.Remove(entity.Id, out var charId))
        {
            _byCharId.Remove(charId);
            return;
        }

        foreach (var kv in _byCharId)
        {
            if (kv.Value.Equals(entity))
            {
                _byCharId.Remove(kv.Key);
                _entityToChar.Remove(entity.Id);
                break;
            }
        }
    }

    public bool TryGetByCharId(int characterId, out Entity entity)
        => _byCharId.TryGetValue(characterId, out entity);

    public bool TryGetByEntityId(int entityId, out Entity entity)
    {
        entity = default;
        return _entityToChar.TryGetValue(entityId, out var charId) 
               && _byCharId.TryGetValue(charId, out entity);
    }
    public IReadOnlyCollection<int> GetAllCharIds() 
        => _byCharId.Keys;
    public bool TryGetCharIdByEntityId(int entityId, out int charId) 
        => _entityToChar.TryGetValue(entityId, out charId);
}