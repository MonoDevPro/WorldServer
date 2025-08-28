// SpatialIndexCommitSystem.cs
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Abstractions.Commons;
using Simulation.Core.Abstractions.Ports;

namespace Simulation.Core.Systems
{
    public sealed partial class SpatialIndexCommitSystem(World world, ISpatialIndex grid) : BaseSystem<World, float>(world)
    {

        // Coleta todos os SpatialIndexDirty e enfileira no próprio grid (defensivamente)
        [Query]
        [All<SpatialDirty>]
        [All<MapId>]
        private void CollectDirty(in Entity e, in SpatialDirty dirty, in MapId map)
        {
            // Enfilera no grid (se ainda não enfileirado a mesma entidade)
            grid.EnqueueUpdate(e.Id, map.Value, dirty.Old, dirty.New);

            // removemos o marker: a própria Flush() não precisa do componente
            World.Remove<SpatialDirty>(e);
        }

        // Atenção: para aplicar todos de uma vez, garantimos que este método seja executado
        // depois que CollectDirty rodar para todas as entidades. Dependendo do scheduler da sua engine,
        // pode ser necessário ordenar os systems para que este esteja numa fase "late".
        // Como medida prática, chamamos Flush aqui — se CollectDirty foi executado para várias entidades
        // neste mesmo frame, Flush irá aplicar tudo que estiver no _pending.
        [Query]
        private void FlushAll()
        {
            // Uma chamada simples para aplicar todos os updates pendentes
            grid.Flush();
        }
    }
}