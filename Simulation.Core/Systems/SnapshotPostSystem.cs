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
    [All<GameSnapshot>]
    private void ProcessGameSnapshot(in Entity entity, in GameSnapshot snapshot)
    {
        // Envia o game snapshot para o EventBus processar e enviar para o jogador que entrou no jogo
        EventBus.Send(snapshot);
        World.Destroy(entity);
    }
    
    [Query]
    [All<CharExitSnapshot>]
    private void ProcessCharExitSnapshot(in Entity entity, in CharExitSnapshot snapshot)
    {
        EventBus.Send(snapshot);
        World.Destroy(entity);
    }
    
    [Query]
    [All<MoveSnapshot>]
    [All<MapRef>]
    [All<TilePosition>]
    private void ProcessMoveSnapshot(in Entity entity, in MoveSnapshot snapshot, in MapRef mapRef)
    {
        EventBus.Send(snapshot);
        World.Remove<MoveSnapshot>(entity);
    }

    [Query]
    [All<AttackSnapshot>]
    [All<MapRef>]
    [All<TilePosition>]
    private void ProcessAttackSnapshot(in Entity entity, in AttackSnapshot snapshot, in MapRef mapRef)
    {
        EventBus.Send(snapshot);
        World.Remove<AttackSnapshot>(entity);
    }
}