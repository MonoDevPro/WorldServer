using Arch.Core;
using Simulation.Core.Abstractions.Adapters.Data;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Core.Utilities.Helpers;

public static class SpawnsHelper
{
    /// <summary>
    /// Cria uma entidade para um character, anexa CharId + CharMetaRef e registra nos índices.
    /// </summary>
    public static Entity SpawnCharacter(World world, CharTemplate template, ICharIndex index, EntityIndex entityIndex, ISpatialIndex spatialIndex)
    {
        var e = world.Create(
            // --- Componentes Adicionados para uma Entidade Completa ---
            template.CharId,
            template.MapId,
            template.Position,
            template.Direction,
            template.MoveStats,
            template.AttackStats,
            template.Blocking
        );
        entityIndex.Register(template.CharId.Value, e);
        spatialIndex.Register(e.Id, template.MapId.Value, template.Position.Value);
        return e;
    }

    public static void DespawnCharacter(World world, Entity entity, EntityIndex entityIndex, ISpatialIndex spatialIndex)
    {
        if (entityIndex.TryGetByEntityId(entity.Id, out var ent))
        {
            if (entityIndex.TryGetCharIdByEntityId(entity.Id, out var charId))
            {
                entityIndex.UnregisterByCharId(charId);
            }
        }

        // best-effort spatial cleanup (if registered)
        if (spatialIndex.IsRegistered(entity.Id))
        {
            var mapId = spatialIndex.GetEntityMap(entity.Id) ?? -1;
            if (mapId >= 0) spatialIndex.Unregister(entity.Id, mapId);
        }

        world.Destroy(entity);
    }
}