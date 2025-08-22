using Arch.Core;
using Arch.Core.Utils;
using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Commons;
using Simulation.Core.Components;

namespace Simulation.Core.Utilities;

public class BlockingIndex
{
    private readonly Dictionary<int, HashSet<GameVector2>> _byMap = new();
    private bool _dirty = true;

    public virtual void MarkDirty() => _dirty = true;

    public virtual void RebuildIfDirty(World world)
    {
        if (!_dirty) return;
        _byMap.Clear();
        var q = new QueryDescription().WithAll<Blocking, TilePosition, MapRef>();
        world.Query(in q, (ref Blocking _, ref TilePosition p, ref MapRef m) =>
            {
                if (!_byMap.TryGetValue(m.MapId, out var set))
                {
                    set = new();
                    _byMap[m.MapId] = set;
                }
                set.Add(p.Position);
            });
        _dirty = false;
    }

    public virtual bool IsBlocked(int mapId, GameVector2 vector2)
    {
        return _byMap.TryGetValue(mapId, out var set) && set.Contains(vector2);
    }
}
