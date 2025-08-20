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
    
    public override void Update(in float t)
    {
        // Em cada frame, a query vai passar por todas as entidades com posição e mapa
        // e a lógica em grid.Update() vai garantir que elas sejam movidas para a célula correta se necessário.
        UpdateGridQuery(World);

        // Opcional: Adicionar lógica para remover entidades destruídas do grid
        // Isso pode ser feito escutando eventos de destruição ou com uma query de "entidades mortas".
    }
}