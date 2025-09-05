using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Application.Ports.ECS.Utils.Indexers;
using Simulation.Domain.Components;

namespace Simulation.Application.Services.ECS.Systems;

/// <summary>
/// Sistema que roda no final do pipeline para sincronizar o estado do ISpatialIndex
/// com as posições atualizadas das entidades no mundo ECS.
/// </summary>
public sealed partial class SpatialIndexSyncSystem(World world, ISpatialIndex spatialIndex)
    : BaseSystem<World, float>(world)
{
    /// <summary>
    /// Encontra todas as entidades marcadas como 'SpatialDirty', atualiza sua posição
    /// no índice espacial e remove a marca.
    /// </summary>
    [Query]
    [All<SpatialDirty, Position>] // A query precisa da Posição para saber o novo valor
    private void SyncDirtyEntities(in Entity entity, in Position position)
    {
        // 1. Notifica o índice espacial sobre a nova posição da entidade.
        spatialIndex.Update(entity, position);

        // 2. Remove o marcador 'dirty', pois a sincronização foi concluída.
        World.Remove<SpatialDirty>(entity);
    }
}