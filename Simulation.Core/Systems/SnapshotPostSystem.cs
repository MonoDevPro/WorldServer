using Arch.Buffer;
using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Abstractions.Out;
using Simulation.Core.Adapters.Out;
using Simulation.Core.Components;
using Simulation.Core.Utilities;

namespace Simulation.Core.Systems;

public partial class SnapshotPostSystem(World world, BoundsIndex mapBounds) 
    : BaseSystem<World, float>(world)
{
    [Query]
    [All<MoveSnapshot>]
    [All<MapRef>]
    [All<TilePosition>]
    private void ProcessMoveSnapshot(in Entity entity, in MoveSnapshot snapshot, in MapRef mapRef,
        ref TilePosition tilePos)
    {
        // Verifica se o mapa está carregado
        if (!mapBounds.TryGet(mapRef.MapId, out var bounds))
            return; // Mapa não encontrado, não processa o snapshot
        
        // Verifica se houve mudança de posição
        if (tilePos.Position == snapshot.Position)
            // Posição não mudou, não processa o snapshot e remove o comando
        {
            World.Remove<MoveSnapshot>(entity);
            return;
        }

        EventBus.Send(snapshot);
    }
}