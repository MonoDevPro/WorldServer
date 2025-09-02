using Arch.Core;
using Simulation.Application.DTOs;
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
        var characterSnapshots = new List<CharTemplate>();

        world.Query(in CharFactory.QueryDescription, (Entity entity, ref MapId mid) =>
        {
            if (mid.Value == mapId)
                characterSnapshots.Add(CharFactory.CreateCharTemplate(world, entity));
        });

        return new EnterSnapshot(mapId: mapId, charId: charId, templates: characterSnapshots.ToArray());
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