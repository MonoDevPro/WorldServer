using Arch.Core;
using Simulation.Core.Abstractions.Adapters.Char;
using Simulation.Core.Abstractions.Commons;

namespace Simulation.Core.Abstractions.Adapters;

public static class SnapshotBuilder
{
    public static EnterSnapshot CreateEnterSnapshot(World world, Entity newEntity)
    {
        if (!world.IsAlive(newEntity))
            throw new ArgumentException("Entity is not alive in the world.", nameof(newEntity));
        if (!world.Has<MapId>(newEntity) || !world.Has<CharId>(newEntity))
            throw new ArgumentException("Entity must have MapId and CharId components.", nameof(newEntity));

        var mapId = world.Get<MapId>(newEntity).Value;
        var charId = world.Get<CharId>(newEntity).Value;
        var characterSnapshots = new List<CharTemplate>();

        world.Query(in CharFactory.QueryDescription, (ref Entity entity, ref MapId entityMapId) =>
        {
            if (entityMapId.Value == mapId)
                characterSnapshots.Add(CharFactory.CreateCharTemplate(world, entity));
        });

        return new EnterSnapshot(mapId: mapId, charId: charId, templates: characterSnapshots.ToArray());
    }

    public static CharSnapshot CreateCharSnapshot(World world, Entity entity)
    {
        if (!world.IsAlive(entity))
            throw new ArgumentException("Entity is not alive in the world.", nameof(entity));
        if (!world.Has<MapId>(entity) || !world.Has<CharId>(entity))
            throw new ArgumentException("Entity must have MapId and CharId components.", nameof(entity));

        var mapId = world.Get<MapId>(entity).Value;
        var charId = world.Get<CharId>(entity).Value;
        return new CharSnapshot(mapId, charId, CharFactory.CreateCharTemplate(world, entity));
    }
    
    public static ExitSnapshot CreateExitSnapshot(int charId) => new(charId);
}