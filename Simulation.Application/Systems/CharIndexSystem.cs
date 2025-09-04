using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Application.DTOs;
using Simulation.Application.Ports.Char.Indexers;
using Simulation.Application.Ports.Map;
using Simulation.Domain.Components;

namespace Simulation.Application.Systems;

public sealed partial class CharIndexSystem(World world, ICharIndex charIndex, ISpatialIndex spatialIndex) : BaseSystem<World, float>(world)
{
    // Roda quando uma entidade com CharId é criada
    [Query]
    [All<EnterIntent>]
    [All<CharId>]
    [All<Position>]
    [None<Indexed>]
    private void OnCharAdded(in Entity entity, in CharId cid, in Position pos)
    {
        charIndex.Register(cid.Value, entity);
        spatialIndex.Add(entity, pos);
        
        World.Add<Indexed>(entity); // Marca como já indexado
    }

    [Query]
    [All<ExitIntent>]
    [All<CharId>]
    [All<Indexed>]
    private void OnCharRemoved(in Entity e, in CharId charId)
    {
        charIndex.Unregister(charId.Value);
        spatialIndex.Remove(e);
        
        World.Remove<Indexed>(e); // Remove a marcação de indexado
        
        // Adiciona tag para salvar dados antes de destruir a entidade
        World.Add<NeedSave>(e);
    }
}