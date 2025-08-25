using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Abstractions.Commons.Components;
using Simulation.Core.Abstractions.Intents.Out;

namespace Simulation.Core.Systems;

public partial class SnapshotPostSystem(World world) 
    : BaseSystem<World, float>(world)
{
    [Query]
    [All<MoveSnapshot>]
    [All<MapRef>]
    [All<TilePosition>]
    private void ProcessMoveSnapshot(in Entity entity, in MoveSnapshot snapshot, in MapRef mapRef,
        ref TilePosition tilePos)
    {
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