using Arch.Core;
using Arch.Core.Utils;
using Simulation.Core.Components;

namespace Simulation.Core.Utilities;

public class BoundsIndex
{
    private readonly Dictionary<int, Bounds> _byMap = new();
    private bool _dirty = true;

    public virtual void MarkDirty() => _dirty = true;

    public virtual void RebuildIfDirty(World world)
    {
        if (!_dirty) return;
        _byMap.Clear();
        var q = new QueryDescription().WithAll<Bounds, MapRef>();
        world.Query(in q, (ref Bounds b, ref MapRef m) =>
            {
                _byMap[m.MapId] = b;
            });
        _dirty = false;
    }

    public virtual bool TryGet(int mapId, out Bounds bounds) => _byMap.TryGetValue(mapId, out bounds);
}
