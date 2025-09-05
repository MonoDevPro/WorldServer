using Arch.Core;
using Simulation.Application.Ports.ECS.Utils;
using Simulation.Domain.Components;

namespace Simulation.Application.Services.ECS.Utils;

public class MapEntityProvider : IMapEntityProvider
{
    // QueryDescription could be cached static if stable
    private static readonly QueryDescription CharQuery = new QueryDescription().WithAll<CharId, MapId>();

    public IEnumerable<Entity> GetEntitiesInMap(World world, int mapId)
    {
        var list = new List<Entity>();

        world.Query(CharQuery, (ref Entity e, ref CharId cid, ref MapId mid) =>
        {
            if (mid.Value == mapId)
                list.Add(e);
        });

        return list;
    }
}