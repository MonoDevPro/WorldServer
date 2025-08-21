using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Components;
using Simulation.Core.Utilities;

namespace Simulation.Core.Systems;

/// <summary>
/// Sistema responsável por manter o SpatialHashGrid sincronizado com as posições das entidades no mundo.
/// </summary>
public sealed partial class IndexUpdateSystem(World world, SpatialHashGrid grid) : BaseSystem<World, float>(world)
{
    // Query para encontrar todas as entidades que devem estar no grid.
    [Query]
    [All<TilePosition, MapRef>]
    private void UpdateGrid(in Entity entity, ref TilePosition pos, ref MapRef map)
    {
        grid.Update(entity, map.MapId, pos.Position);
    }
}