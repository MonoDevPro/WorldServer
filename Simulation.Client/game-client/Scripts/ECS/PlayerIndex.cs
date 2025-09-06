using Arch.Core;

namespace GameClient.Scripts.ECS;

public class PlayerIndex
{
    private readonly Dictionary<int, Entity> _byChar = new();
    public void Add(int charId, Entity e) => _byChar[charId] = e;
    public bool TryGet(int charId, out Entity e) => _byChar.TryGetValue(charId, out e);
    public void Remove(int charId) => _byChar.Remove(charId);
    public void Clear() => _byChar.Clear();
}
