using Arch.Core;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Char.Indexers;
using Simulation.Application.Utilities;
using Simulation.Domain.Components;
using Simulation.Domain.Templates;

namespace Simulation.Application.Factories;

public static class SnapshotBuilder
{
    public static EnterSnapshot CreateEnterSnapshot(World world, Entity newEntity, CharTemplate[]? existingTemplates = null)
    {
        if (!world.IsAlive(newEntity))
            throw new ArgumentException("Entity is not alive in the world.", nameof(newEntity));
        if (!world.Has<MapId>(newEntity) || !world.Has<CharId>(newEntity))
            throw new ArgumentException("Entity must have MapId and CharId components.", nameof(newEntity));

        var mapId = world.Get<MapId>(newEntity).Value;
        var charId = world.Get<CharId>(newEntity).Value;
        
        // If existing templates are provided, use them directly to avoid duplication
        if (existingTemplates != null)
        {
            return new EnterSnapshot(mapId: mapId, charId: charId, templates: existingTemplates);
        }
        
        // Use object pool for the character list to reduce allocations
        var characterSnapshots = ListPool.Get();
        try
        {
            world.Query(in CharFactory.QueryDescription, (Entity entity, ref MapId mid) =>
            {
                if (mid.Value == mapId)
                    characterSnapshots.Add(CharFactory.CreateCharTemplate(world, entity));
            });

            return new EnterSnapshot(mapId: mapId, charId: charId, templates: TemplateArrayPool.CreateExactArray(characterSnapshots));
        }
        finally
        {
            // Return the list to the pool for reuse
            ListPool.Return(characterSnapshots);
        }
    }

    /// <summary>
    /// Creates an EnterSnapshot with template index optimization
    /// </summary>
    public static EnterSnapshot CreateEnterSnapshotOptimized(World world, Entity newEntity, 
        ICharTemplateIndex templateIndex)
    {
        if (!world.IsAlive(newEntity))
            throw new ArgumentException("Entity is not alive in the world.", nameof(newEntity));
        if (!world.Has<MapId>(newEntity) || !world.Has<CharId>(newEntity))
            throw new ArgumentException("Entity must have MapId and CharId components.", nameof(newEntity));

        var mapId = world.Get<MapId>(newEntity).Value;
        var charId = world.Get<CharId>(newEntity).Value;
        
        // Use object pool for the character list to reduce allocations
        var characterSnapshots = ListPool.Get();
        try
        {
            world.Query(in CharFactory.QueryDescription, (Entity entity, ref MapId mid) =>
            {
                if (mid.Value == mapId)
                {
                    var entityCharId = world.Get<CharId>(entity).Value;
                    
                    // Try to get cached template first, only create new one if needed
                    if (templateIndex.TryGet(entityCharId, out var cachedTemplate))
                    {
                        // Update cached template with current entity state to ensure freshness
                        CharFactory.UpdateCharTemplate(world, entity, cachedTemplate);
                        characterSnapshots.Add(cachedTemplate);
                    }
                    else
                    {
                        // Create new template if not in cache
                        var newTemplate = CharFactory.CreateCharTemplate(world, entity);
                        characterSnapshots.Add(newTemplate);
                    }
                }
            });

            return new EnterSnapshot(mapId: mapId, charId: charId, templates: TemplateArrayPool.CreateExactArray(characterSnapshots));
        }
        finally
        {
            // Return the list to the pool for reuse
            ListPool.Return(characterSnapshots);
        }
    }

    public static CharSnapshot CreateCharSnapshot(World world, Entity entity, CharTemplate? existingTemplate = null)
    {
        if (!world.IsAlive(entity))
            throw new ArgumentException("Entity is not alive in the world.", nameof(entity));
        if (!world.Has<MapId>(entity) || !world.Has<CharId>(entity))
            throw new ArgumentException("Entity must have MapId and CharId components.", nameof(entity));

        var mapId = world.Get<MapId>(entity).Value;
        var charId = world.Get<CharId>(entity).Value;
        
        // Se um template existente for fornecido, atualize-o. Caso contrário, crie um novo.
        var template = existingTemplate ?? CharFactory.CreateCharTemplate(world, entity);

        return new CharSnapshot(mapId, charId, template);
    }


    public static ExitSnapshot CreateExitSnapshot(World world, Entity entity, CharTemplate? existingTemplate = null)
    {
        if (!world.IsAlive(entity))
            throw new ArgumentException("Entity is not alive in the world.", nameof(entity));
        if (!world.Has<MapId>(entity) || !world.Has<CharId>(entity))
            throw new ArgumentException("Entity must have MapId and CharId components.", nameof(entity));

        var mapId = world.Get<MapId>(entity).Value;
        var charId = world.Get<CharId>(entity).Value;
        
        // Se um template existente for fornecido, atualize-o. Caso contrário, crie um novo.
        var template = existingTemplate is not null
            ? CharFactory.UpdateCharTemplate(world, entity, existingTemplate)
            : CharFactory.CreateCharTemplate(world, entity);
        
        // Supondo que o DTO ExitSnapshot agora é: record struct ExitSnapshot(int MapId, int CharId, CharTemplate Template)
        return new ExitSnapshot(mapId, charId, template);
    }
}